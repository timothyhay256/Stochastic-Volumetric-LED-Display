use indicatif::{ProgressBar, ProgressState, ProgressStyle};
use log::{debug, error, info};
use opencv::{
    core::{Point, Scalar},
    imgproc::{self, LINE_8},
    videoio::{self, VideoCaptureTrait, VideoCaptureTraitConst},
};
use std::{
    collections::HashMap,
    error::Error,
    fs::File,
    io::prelude::*,
    net::{Ipv4Addr, TcpStream, UdpSocket},
    str,
    sync::{Arc, Mutex},
    time::Duration,
};

use crate::led_manager;
use crate::Config;
use crate::ManagerData;
use crate::UnityOptions;

type JsonEntry = Vec<(String, (f32, f32), (f32, f32))>;

pub fn signal_restart(unity_ip: Ipv4Addr, unity_port: u32) {
    let mut stream = match TcpStream::connect(format!("{}:{}", unity_ip, unity_port)) {
        Ok(stream) => stream,
        Err(e) => {
            panic!("Could not establish connection on {unity_ip}:{unity_port} with Unity: {e}")
        }
    };
    stream
        .set_read_timeout(Some(Duration::new(0, 1000000000)))
        .unwrap();

    match stream.write_all("RESTART".as_bytes()) {
        Ok(_) => {}
        Err(e) => {
            panic!("Could not signal restart: {e}")
        }
    };
}

pub fn send_pos(unity: UnityOptions) -> std::io::Result<()> {
    for mut i in 1..=unity.num_container {
        i -= 1; // TODO: There is def a better way
        debug!(
            "sending pos file {:?}",
            unity.unity_position_files[i as usize]
        );
        let mut pos_file = match File::open(unity.unity_position_files[i as usize].clone()) {
            Ok(file) => file,
            Err(e) => {
                panic!(
                    "Could not read {:?}: {}",
                    unity.unity_position_files[i as usize], e
                )
            }
        };

        let mut file_contents = String::new();
        match pos_file.read_to_string(&mut file_contents) {
            Ok(_) => {}
            Err(e) => {
                panic!(
                    "Could not read position file {}: {}",
                    unity.unity_position_files[i as usize].display(),
                    e
                )
            }
        };

        let json: JsonEntry = match serde_json::from_str(&file_contents) {
            Ok(json) => json,
            Err(e) => {
                panic!(
                    "{} contains invalid or incomplete calibration data: {}",
                    unity.unity_position_files[i as usize].display(),
                    e
                )
            }
        };

        let pb = ProgressBar::new(json.len().try_into().unwrap());
        pb.set_style(ProgressStyle::with_template("{spinner:.green} [{elapsed_precise}] [{wide_bar:.cyan/blue}] {pos:>3}/{len:3} ({eta})")
        .unwrap()
        .with_key("eta", |state: &ProgressState, w: &mut dyn std::fmt::Write| write!(w, "{:.1}s", state.eta().as_secs_f64()).unwrap())
        .progress_chars("#>-")); // This can take a while, especially for alot of LEDs
        let mut pb_count = 0;

        debug!("establishing connection to unity");
        let mut stream = TcpStream::connect(format!(
            "{}:{}",
            unity.unity_ip.clone(),
            unity.unity_ports.clone()[i as usize]
        ))?;
        stream
            .set_read_timeout(Some(Duration::new(0, 1000000000)))
            .unwrap();
        stream
            .set_write_timeout(Some(Duration::new(0, 1000000000)))
            .unwrap();
        debug!("sending positions to connection");
        for led in json.iter() {
            pb_count += 1;
            pb.set_position(pb_count);
            stream.write_all(
                format!(
                    "{},{},{}",
                    led.1 .0 * unity.scale,
                    led.1 .1 * unity.scale,
                    led.2 .0 * unity.scale
                )
                .as_bytes(),
            )?;
            let mut response: [u8; 3] = [0; 3];
            stream.read_exact(&mut response)?;

            if match str::from_utf8(&response) {
                Ok(v) => v,
                Err(e) => panic!("Invalid UTF-8 sequence: {}", e),
            } != "ack"
            {
                error!("Did not get acknowledgement from Unity! You may have missing LEDs.");
            }
        }
        pb.finish();

        stream.write_all("END".as_bytes())?;
    }
    Ok(())
}

pub fn get_events(
    manager: &Arc<Mutex<ManagerData>>,
    unity: &UnityOptions,
    config: &Config,
    port: &u32,
) -> Result<(), Box<dyn Error>> {
    type JsonHashmap = HashMap<usize, ((f32, f32), (f32, f32), (u8, u8, u8), bool)>; // <index, xy, zy, rgb, illuminated>

    let ip = unity.unity_ip;

    debug!("get_events active on {}:{}", ip, port);
    let socket = UdpSocket::bind(format!("{}:{}", ip, port))?;

    // load positions if we are streaming video with widgets
    let json: JsonEntry;
    let mut json_hashmap: JsonHashmap = Default::default();

    if config.get_events_video_widgets {
        // If one isn't set, assume the first pos file
        let pos_index = config.get_events_widgets_pos_index.unwrap_or(0);

        let mut pos_file = match File::open(unity.unity_position_files[pos_index as usize].clone())
        {
            Ok(file) => file,
            Err(e) => {
                panic!(
                    "Could not read {:?}: {}",
                    unity.unity_position_files[pos_index as usize], e
                )
            }
        };

        let mut file_contents = String::new();
        match pos_file.read_to_string(&mut file_contents) {
            Ok(_) => {}
            Err(e) => {
                panic!(
                    "Could not read position file {}: {}",
                    unity.unity_position_files[pos_index as usize].display(),
                    e
                )
            }
        };

        json = match serde_json::from_str(&file_contents) {
            Ok(json) => json,
            Err(e) => {
                panic!(
                    "{} contains invalid or incomplete calibration data: {}",
                    unity.unity_position_files[pos_index as usize].display(),
                    e
                )
            }
        };

        json_hashmap = json
            .into_iter()
            .enumerate()
            .map(|(i, (_key, val1, val2))| (i, (val1, val2, (0u8, 0u8, 0u8), false)))
            .collect();
    }

    loop {
        if !manager.lock().unwrap().keepalive {
            info!("get_events exiting.");
            manager.lock().unwrap().keepalive = true;
            break;
        }

        let mut frame_cam_1 = &mut manager.lock().unwrap().frame_cam_1;
        let mut frame_cam_2 = &mut manager.lock().unwrap().frame_cam_2;

        let cam;
        let cam2;

        if config.get_events_streams_video {
            cam = Some(videoio::VideoCapture::new(
                config.camera_index_1,
                videoio::CAP_ANY,
            )?);
            if config.multi_camera {
                cam2 = Some(videoio::VideoCapture::new(
                    config.camera_index_2.unwrap(),
                    videoio::CAP_ANY,
                )?);
                match videoio::VideoCapture::is_opened(cam2.as_ref().unwrap())? {
                    true => {}
                    false => {
                        panic!("Unable to open camera 2!")
                    }
                };

                cam2.unwrap().read(&mut frame_cam_2)?;
            }
            match videoio::VideoCapture::is_opened(cam.as_ref().unwrap())? {
                true => {}
                false => {
                    panic!("Unable to open camera 1!")
                }
            };
            cam.unwrap().read(&mut frame_cam_1)?;
        }

        let mut buf = [0; 16];
        socket.recv_from(&mut buf)?;
        let msg = match str::from_utf8(&buf) {
            Ok(msg) => msg,
            Err(e) => {
                error!(
                    "Received invalid packet from Unity:{:?} which resulted in the following: {}",
                    buf, e
                );
                "FAIL"
            }
        };
        let mut msg = msg.to_string();
        if msg.contains("E") {
            println!("{msg}");
            // Clear color of index `EN`
            msg.remove(0);
            let index = match msg.to_string().parse::<u16>() {
                Ok(index) => index,
                Err(e) => {
                    panic!(
                        "Unity packet was malformed: Attempted to convert {} to u8: {}",
                        msg, e
                    )
                }
            };
            led_manager::set_color(manager, index, 0, 0, 0);

            // Indicate this isn't illuminated
            if let Some(value) = json_hashmap.get_mut(&(index as usize)) {
                value.3 = false;
            }
        } else if msg.contains("|") {
            // Set index n with r g b from string n|r|g|b
            let mut xs: [u16; 4] = [0; 4];
            let nrgb = msg.trim_matches(char::is_control).split("|");
            for (i, el) in nrgb.enumerate() {
                xs[i] = match el.parse::<u16>() {
                    Ok(el) => el,
                    Err(e) => {
                        panic!(
                            "Unity packet was malformed: Attempted to convert {} to u8: {}",
                            el, e
                        )
                    }
                };
            }
            // println!("NRGB: {}|{}|{}|{}", xs[0], xs[1], xs[2], xs[3]);

            // Indicate this is illuminated
            if let Some(value) = json_hashmap.get_mut(&(xs[0] as usize)) {
                value.3 = true;
                value.2 = (xs[1] as u8, xs[2] as u8, xs[3] as u8);
            }
            led_manager::set_color(manager, xs[0], xs[1] as u8, xs[2] as u8, xs[3] as u8);
        } else {
            error!("Unity packet was malformed! Packet: {}", msg);
        }

        if config.get_events_video_widgets {
            for (_key, (xy, z, rgb, _enabled)) in json_hashmap
                .iter()
                .filter(|(_, (_xy, _z, _rgb, enabled))| *enabled)
            {
                debug!("applying circles to frames");

                imgproc::circle(
                    &mut frame_cam_1,
                    Point::new(xy.0 as i32, xy.1 as i32),
                    20,
                    Scalar::new(0.0f64, rgb.2 as f64, rgb.1 as f64, rgb.0 as f64),
                    2,
                    LINE_8,
                    0,
                )?;

                imgproc::circle(
                    &mut frame_cam_1,
                    Point::new(z.1 as i32, xy.1 as i32),
                    20,
                    Scalar::new(0.0f64, rgb.2 as f64, rgb.1 as f64, rgb.0 as f64),
                    2,
                    LINE_8,
                    0,
                )?;
            }
        }
    }

    Ok(())
}
