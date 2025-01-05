use log::{debug, error, info, warn};
use std::{
    env,
    io::{BufWriter, ErrorKind::WouldBlock, Write},
    net::UdpSocket,
    path::{Path, PathBuf},
    process,
    time::{Duration, SystemTime},
};

use crate::ManagerData;

pub fn set_color(manager: &mut ManagerData, n: u8, r: u8, g: u8, b: u8) {
    // &mut should mean changes will persist, so no need to return ManagerData
    let record_data;
    let record_esp_data;

    if manager.unity_controls_recording {
        // TODO: Find better solution for this.
        let unity_start_anim_path: PathBuf = [env::temp_dir().to_str().unwrap(), "start_animate"]
            .iter()
            .collect();
        record_data = Path::new(&unity_start_anim_path.into_os_string()).exists();

        let unity_start_anim_byte_path: PathBuf =
            [env::temp_dir().to_str().unwrap(), "start_animate_byte"]
                .iter()
                .collect();
        record_esp_data = Path::new(&unity_start_anim_byte_path.into_os_string()).exists();
    } else {
        record_data = manager.record_data;
        record_esp_data = manager.record_esp_data;
    }

    if manager.first_run {
        manager.first_run = false;
        manager.call_time = SystemTime::now();
    }

    if record_data || record_esp_data {
        if record_data && manager.data_file_buf.is_none() {
            manager.data_file_buf = Some(BufWriter::new(
                match crate::check_and_create_file(&manager.record_data_file) {
                    Ok(file) => file,
                    Err(e) => {
                        panic!(
                            "Could not open {} for writing animation: {}",
                            manager.record_data_file.display(),
                            e
                        );
                    }
                },
            ));
        } else if record_esp_data && manager.esp_data_file_buf.is_none() {
            manager.esp_data_file_buf = Some(BufWriter::new(
                match crate::check_and_create_file(&manager.record_esp_data_file) {
                    Ok(file) => file,
                    Err(e) => {
                        panic!(
                            "Could not open {} for writing animation: {}",
                            manager.record_esp_data, e
                        )
                    }
                },
            ));
        }
        let end = SystemTime::now();
        match end.duration_since(manager.call_time) {
            Ok(duration) => {
                manager.call_time = SystemTime::now(); // Reset timer
                let mut millis = duration.as_millis();
                if millis >= 1 {
                    if record_data {
                        match manager.data_file_buf.as_mut() {
                            Some(data_file_buf) => {
                                writeln!(data_file_buf, "T:{}", &millis.to_string()).expect("Could not write to data_file_buf!");
                                if n == 1 && r == 2 && g == 3 && b == 4 {
                                    warn!("Modifying instruction to disk by 1 to prevent parsing error!"); // This is a timing instruction, so we cannot let it be written.
                                    writeln!(data_file_buf, "{}|{}|{}|{}", n, r + 1, g, b).expect("Could not write to data_file_buf!");
                                } else {
                                    writeln!(data_file_buf, "{}|{}|{}|{}", n, r, g, b).expect("Could not write to data_file_buf!");
                                }
                            }
                            None => error!("record_data is true, but data_file_buf is None! Something has gone very wrong, please report this.")
                        }
                    }
                    if record_esp_data {
                        match manager.esp_data_file_buf.as_mut() {
                            Some(esp_data_file_buf) => {
                                while millis > 255 {
                                    // Adds overflows where we can't store above 255 ms
                                    // debug!("Detected integer overflow, adding to other element");
                                    for i in 1..=5 {
                                        // Indicates a timing instruction, as it is unlikely that LED 1 will be set to 2,3,4 (r,g,b)
                                        write!(esp_data_file_buf, "{:#x}, ", i).expect("Could not write to esp_data_file_buf!");
                                    }
                                    write!(esp_data_file_buf, "{:#x}, ", 255).expect("Could not write to esp_data_file_buf!");

                                    millis -= 255;
                                }
                                if millis > 0 {
                                    // debug!("No longer or not overflow.");
                                    for i in 1..=5 {
                                        write!(esp_data_file_buf, "{:#x}, ", i).expect("Could not write to esp_data_file_buf!");
                                    }
                                    write!(esp_data_file_buf, "{:#x}, ", millis).expect("Could not write to esp_data_file_buf!");
                                }
                                write!(esp_data_file_buf, "{:#x}, {:#x}, {:#x}, {:#x}, ", n, r, g, b).expect("Could not write to esp_data_file_buf!");
                            }
                            None => error!("record_esp_data is true, but esp_data_file_buf is None!, Something has gone very wrong, please report this.")
                        }
                    }
                }
            }
            Err(e) => println!("Error: {}", e),
        }
    }

    if manager.communication_mode == 1 {
        if manager.udp_socket.is_none() {
            debug!("Binding to 0.0.0.0:{}", manager.port);
            manager.udp_socket = Some(match UdpSocket::bind(format!("0.0.0.0:{}", manager.port)) {
                Ok(socket) => socket,
                Err(e) => {
                    panic!("Could not bind to 0.0.0.0:{}: {}", manager.port, e);
                }
            });
        }
        match manager.udp_socket.as_mut() {
            Some(udp_socket) => {
                udp_socket
                    .set_read_timeout(Some(Duration::new(0, manager.udp_read_timeout * 1000000)))
                    .expect("set_read_timeout call failed");

                let bytes: [u8; 4] = [n, r, g, b];
                // debug!("Sending {:?}", bytes);
                match udp_socket.send_to(&bytes, format!("{}:{}", manager.host, manager.port)) {
                    Ok(_) => {}
                    Err(e) => {
                        panic!("Could not write bytes to UDP socket: {}", e)
                    }
                }
                let mut buf = [0; 3];
                let udp_result = udp_socket.recv(&mut buf);

                match udp_result {
                    // TODO: This is untested! Test it
                    Ok(_size) => {
                        manager.failures = 0; // Reset consecutive failure count
                    }
                    Err(ref e) if e.kind() == WouldBlock => {
                        if manager.failures >= manager.con_fail_limit {
                            error!("Too many consecutive communication failures, exiting.");
                            process::exit(1);
                        }
                        warn!(
                            "UDP timeout reached! Will resend packet, but won't wait for response!"
                        );
                        match udp_socket
                            .send_to(&bytes, format!("{}:{}", manager.host, manager.port))
                        {
                            Ok(_) => {}
                            Err(e) => {
                                panic!("Could not write bytes to UDP socket: {}", e)
                            }
                        }
                        manager.failures += 1
                    }
                    Err(e) => {
                        error!("An error occurred sending data: {}", e);
                    }
                }

                if buf == [42, 41, 44] {
                    // "BAD" - indicates the remote device reported a malformed packet
                    warn!("ESP reported a malformed packet!"); // TODO: Should we resend packet and not wait?
                    manager.failures += 1
                }
            }
            None => panic!("Could not send packet as manager.udp_socket does not exist!"),
        };
    } else if manager.communication_mode == 2 {
        if manager.serial_port.is_none() {
            manager.serial_port = Some(
                match serialport::new(manager.serial_port_path.clone(), manager.baud_rate)
                    .timeout(Duration::from_millis(manager.serial_read_timeout.into()))
                    .open()
                {
                    Ok(port) => port,
                    Err(e) => panic!("Could not open {}: {}", manager.serial_port_path, e),
                },
            );
        }
        if let Some(serial_port) = manager.serial_port.as_mut() {
            let msg: [u8; 6] = [0xFF, 0xBB, n, r, g, b]; // 0xFF & 0xBB indicate a start of packet.
            match serial_port.write_all(&msg) {
                Ok(_) => {}
                Err(e) => {
                    panic!(
                        "Could not write bytes to {}:{}",
                        manager.serial_port_path, e
                    )
                }
            }

            if manager.print_send_back {
                let mut serial_buf: Vec<u8> = vec![0; 7];

                let read_result = serial_port.read(serial_buf.as_mut_slice());

                match read_result {
                    Ok(_size) => {
                        info!("print_send_back returned {:?}", serial_buf);
                    }
                    Err(e) => {
                        error!("print_send_back could not read serial port: {}", e);
                    }
                };
            } else {
                let mut failures = 0;
                let mut serial_buf: Vec<u8> = vec![0; 1];

                while serial_buf != [0x01] {
                    match serial_port.read_exact(serial_buf.as_mut_slice()) {
                        Ok(_) => {}
                        Err(e) => {
                            error!("Could not read from {}: {}", manager.serial_port_path, e)
                        }
                    }
                    failures += 1;

                    if failures >= manager.serial_read_timeout {
                        error!(
                            "Did not receive confirmation byte after {}ms! Continuing anyway!",
                            manager.serial_read_timeout
                        );
                        break;
                    }
                }
            }
        };
    }
}
