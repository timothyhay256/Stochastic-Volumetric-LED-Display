use log::error;
use std::error::Error;
use std::fs::File;
use std::io::prelude::*;
use std::net::Ipv4Addr;
use std::net::TcpStream;
use std::net::UdpSocket;
use std::str;

use crate::led_manager;
use crate::ManagerData;
use crate::UnityOptions;

pub fn send_pos(unity: UnityOptions) -> std::io::Result<()> {
    type JsonEntry = Vec<(String, (f32, f32), (f32, f32))>;
    for mut i in 1..=unity.num_container {
        i -= 1; // TODO: There is def a better way
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
        let mut stream = TcpStream::connect(format!(
            "{}:{}",
            unity.unity_ip.clone(),
            unity.unity_ports.clone()[i as usize]
        ))?;
        for led in json.iter() {
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

        stream.write_all("END".as_bytes())?;
    }
    Ok(())
}

pub fn get_events(
    manager: &mut ManagerData,
    ip: Ipv4Addr,
    port: i32,
) -> Result<(), Box<dyn Error>> {
    let socket = UdpSocket::bind(format!("{}:{}", ip, port))?;

    if manager.keepalive {
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
                // Clear color of index `EN`
                msg.remove(0);
                let index = match msg.to_string().parse::<u8>() {
                    Ok(index) => index,
                    Err(e) => {
                        panic!(
                            "Unity packet was malformed: Attempted to convert {} to u8: {}",
                            msg, e
                        )
                    }
                };
                led_manager::set_color(manager, index, 0, 0, 0);
            } else if msg.contains("|") {
                // Set index n with r g b from string n|r|g|b
                let mut xs: [u8; 4] = [0; 4];
                let nrgb = msg.split("|");
                for (i, el) in nrgb.enumerate() {
                    xs[i] = match el.to_string().parse::<u8>() {
                        Ok(el) => el,
                        Err(e) => {
                            panic!(
                                "Unity packet was malformed: Attempted to convert {} to u8: {}",
                                el, e
                            )
                        }
                    };
                }
                led_manager::set_color(manager, xs[0], xs[1], xs[2], xs[3]);
            } else {
                error!("Unity packet was malformed! Packet: {}", msg);
            }
        }
    }
    Ok(())
}
