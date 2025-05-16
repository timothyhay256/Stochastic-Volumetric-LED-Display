use indicatif::{ProgressBar, ProgressState, ProgressStyle};
use log::{debug, error, info, warn};
use opencv::{
    core::{Mat, Point, Scalar},
    imgproc::{self, LINE_8},
    videoio::{
        self, VideoCaptureTrait, VideoCaptureTraitConst, CAP_PROP_FRAME_HEIGHT,
        CAP_PROP_FRAME_WIDTH,
    },
};
use std::{
    cmp::max,
    collections::HashMap,
    error::Error,
    fs::File,
    io::prelude::*,
    net::{Ipv4Addr, TcpStream, UdpSocket},
    str,
    sync::{Arc, Mutex},
    thread,
    time::Duration,
};

use crate::Config;
use crate::ManagerData;
use crate::UnityOptions;
use crate::{led_manager, GetEventsFrameBuffer};

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
        stream.set_read_timeout(Some(Duration::new(1, 0))).unwrap();
        stream.set_write_timeout(Some(Duration::new(1, 0))).unwrap();
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
    manager: Arc<Mutex<ManagerData>>,
    unity: &UnityOptions,
    config: &Config,
    port: &u32,
    frame_buffer: &Option<Arc<Mutex<GetEventsFrameBuffer>>>, // Seperate buffer for frames to reduce locks on manager
) -> Result<(), Box<dyn Error>> {
    type JsonHashmap = HashMap<usize, ((f32, f32), (f32, f32), (u8, u8, u8), bool)>; // <index, xy, zy, rgb, illuminated>

    let ip = unity.unity_ip;

    debug!("get_events active on {}:{}", ip, port);
    let socket = UdpSocket::bind(format!("{}:{}", ip, port))?;

    // load positions if we are streaming video with widgets
    let mut json: JsonEntry;
    let mut json_hashmap: Arc<Mutex<JsonHashmap>> = Arc::new(Mutex::new(Default::default()));

    let mut frame_cam_1: Mat = Default::default();
    let mut frame_cam_2: Mat = Default::default();

    let owned_config = config.clone();
    let owned_manager = Arc::clone(&manager);

    if config.advanced.get_events_video_widgets.unwrap_or(false) {
        // If one isn't set, assume the first pos file
        let pos_index = config.advanced.get_events_widgets_pos_index.unwrap_or(0);

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

        let led_count = json.len();
        let mut y_max = i32::MIN;

        for i in 0..led_count {
            // Get max and min values in led_pos
            y_max = max((json[i].1 .1) as i32, y_max);
        }

        for i in 0..led_count {
            let y_mid = y_max / 2;
            let current_y = json[i].1 .1;

            json[i].1 .1 = match current_y {
                y if y > y_mid as f32 => y_mid as f32 - (y - y_mid as f32),
                y if y < y_mid as f32 => y_mid as f32 + (y_mid as f32 - y),
                _ => json[i].1 .1,
            };
        }

        json_hashmap = Arc::new(Mutex::new(
            json.into_iter()
                .enumerate()
                .map(|(i, (_key, val1, val2))| (i, (val1, val2, (0u8, 0u8, 0u8), false)))
                .collect(),
        ));
    }

    if config
        .advanced
        .get_events_streams_video
        .unwrap_or_else(|| false)
        && frame_buffer.is_some()
    {
        info!("Spawning get_events_streams_video thread.");
        warn!("This should only be used in demos due to decreased performance!");

        let owned_frame_buffer = Arc::clone(frame_buffer.as_ref().unwrap());
        let json_hashmap_guard = Arc::clone(&json_hashmap);

        thread::Builder::new()
            .name("get_events_stream_video".to_string())
            .spawn(move || {
                debug!("Opening cameras!");

                let config = owned_config;
                let mut cam2 = None;

                let cam = Arc::new(Mutex::new(
                    videoio::VideoCapture::new(config.camera_index_1, videoio::CAP_ANY).unwrap(),
                ));

                if config.video_width.is_some() && config.video_height.is_some() {
                    cam.lock()
                        .unwrap()
                        .set(CAP_PROP_FRAME_WIDTH, config.video_width.unwrap())
                        .unwrap();
                    cam.lock()
                        .unwrap()
                        .set(CAP_PROP_FRAME_HEIGHT, config.video_height.unwrap())
                        .unwrap();
                }

                match videoio::VideoCapture::is_opened(cam.as_ref().lock().as_ref().unwrap())
                    .unwrap()
                {
                    true => {}
                    false => {
                        panic!("Unable to open camera 1!")
                    }
                };

                if config.multi_camera {
                    cam2 = Some(Arc::new(Mutex::new(
                        videoio::VideoCapture::new(
                            config.camera_index_2.unwrap(),
                            videoio::CAP_ANY,
                        )
                        .unwrap(),
                    )));

                    if config.video_width.is_some() && config.video_height.is_some() {
                        cam2.as_ref()
                            .unwrap()
                            .lock()
                            .unwrap()
                            .set(CAP_PROP_FRAME_WIDTH, config.video_width.unwrap())
                            .unwrap();
                        cam2.as_ref()
                            .unwrap()
                            .lock()
                            .unwrap()
                            .set(CAP_PROP_FRAME_HEIGHT, config.video_height.unwrap())
                            .unwrap();
                    }

                    match videoio::VideoCapture::is_opened(&cam2.as_ref().unwrap().lock().unwrap())
                        .unwrap()
                    {
                        true => {}
                        false => {
                            panic!("Unable to open camera 2!")
                        }
                    };
                }

                loop {
                    cam.lock().unwrap().read(&mut frame_cam_1).unwrap();

                    if let Some(cam2) = &mut cam2 {
                        if config.multi_camera {
                            cam2.lock().unwrap().read(&mut frame_cam_2).unwrap();
                        }
                    }

                    if config
                        .advanced
                        .get_events_video_widgets
                        .unwrap_or_else(|| false)
                    {
                        for (_key, (xy, z, rgb, _enabled)) in json_hashmap_guard
                            .lock()
                            .unwrap()
                            .iter()
                            .filter(|(_, (_xy, _z, _rgb, enabled))| *enabled)
                        {
                            imgproc::circle(
                                &mut frame_cam_1,
                                Point::new(xy.0 as i32, xy.1 as i32),
                                20,
                                Scalar::new(rgb.2 as f64, rgb.1 as f64, rgb.0 as f64, 0.0f64),
                                2,
                                LINE_8,
                                0,
                            )
                            .unwrap();

                            imgproc::circle(
                                &mut frame_cam_2,
                                Point::new(z.0 as i32, xy.1 as i32),
                                20,
                                Scalar::new(rgb.2 as f64, rgb.1 as f64, rgb.0 as f64, 0.0f64),
                                2,
                                LINE_8,
                                0,
                            )
                            .unwrap();
                        }
                    }

                    {
                        let mut frame_buffer = owned_frame_buffer.lock().unwrap();
                        frame_buffer.shared_frame_1 = frame_cam_1.clone();
                        frame_buffer.shared_frame_2 = frame_cam_2.clone();
                        if !owned_manager.lock().unwrap().state.keepalive {
                            info!("get_events_streams_video thread exiting");
                            break;
                        }
                    }
                }
            })
            .unwrap();
    }

    loop {
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
            // println!("{msg}");
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
            led_manager::set_color(&manager, index, 0, 0, 0);

            // Indicate this isn't illuminated
            if let Some(value) = json_hashmap.lock().unwrap().get_mut(&(index as usize)) {
                value.3 = false;
            }
            info!("dimming {}", index);
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

            if xs[1] != 0 || xs[2] != 0 || xs[3] != 0 {
                // Indicate this is illuminated
                if let Some(value) = json_hashmap.lock().unwrap().get_mut(&(xs[0] as usize)) {
                    value.3 = true;
                    value.2 = (xs[1] as u8, xs[2] as u8, xs[3] as u8);
                }
            } else {
                // Indicate this isn't illuminated
                if let Some(value) = json_hashmap.lock().unwrap().get_mut(&(xs[0] as usize)) {
                    value.3 = false;
                }
            }
            led_manager::set_color(&manager, xs[0], xs[1] as u8, xs[2] as u8, xs[3] as u8);
        } else {
            error!("Unity packet was malformed! Packet: {}", msg);
        }
        if !manager.lock().unwrap().state.keepalive {
            info!("get_events exiting.");
            manager.lock().unwrap().state.keepalive = true;
            break;
        }
    }

    Ok(())
}
