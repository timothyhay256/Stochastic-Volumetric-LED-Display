use crate::ManagerData;
use log::{debug, error, warn};
use std::env;
use std::error::Error;
use std::fs::{remove_file, File};
use std::io::BufWriter;
use std::io::ErrorKind::WouldBlock;
use std::io::Write;
use std::net::UdpSocket;
use std::path::{Path, PathBuf};
use std::time::{Duration, SystemTime};

pub fn set_color(
    manager: &mut ManagerData,
    n: u8,
    r: u8,
    g: u8,
    b: u8,
) -> Result<(), Box<dyn Error>> {
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
                check_and_create_file(&manager.record_data_file).unwrap_or_else(|_| {
                    panic!(
                        "Could not open {} for writing animation!",
                        manager.record_data_file.display()
                    )
                }),
            ));
        } else if record_esp_data && manager.esp_data_file_buf.is_none() {
            manager.esp_data_file_buf = Some(BufWriter::new(
                check_and_create_file(&manager.record_esp_data_file).unwrap_or_else(|_| {
                    panic!(
                        "Could not open {} for writing animation!",
                        manager.record_esp_data_file.display()
                    )
                }),
            ));
        }
        let end = SystemTime::now();
        match end.duration_since(manager.call_time) {
            Ok(duration) => {
                let mut millis = duration.as_millis();
                if millis >= 1 {
                    if record_data {
                        match manager.data_file_buf.as_mut() {
                            Some(data_file_buf) => {
                                writeln!(data_file_buf, "T:{}", &millis.to_string())?;
                                if n == 1 && r == 2 && g == 3 && b == 4 {
                                    warn!("Modifying instruction to disk by 1 to prevent parsing error!"); // This is a timing instruction, so we cannot let it be written.
                                    writeln!(data_file_buf, "{}|{}|{}|{}", n, r + 1, g, b)?;
                                } else {
                                    writeln!(data_file_buf, "{}|{}|{}|{}", n, r, g, b)?;
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
                                    debug!("Detected integer overflow, adding to other element");
                                    for i in 1..=5 {
                                        // Indicates a timing instruction, as it is unlikely that LED 1 will be set to 2,3,4 (r,g,b)
                                        write!(esp_data_file_buf, "{}", i)?;
                                        // TODO: Better way to do this?
                                    }
                                    write!(esp_data_file_buf, "{}", 255)?;

                                    millis -= 255;
                                }
                                if millis > 0 {
                                    debug!("No longer or not overflow.");
                                    for i in 1..=5 {
                                        write!(esp_data_file_buf, "{}", i)?;
                                    }
                                    write!(esp_data_file_buf, "{}", millis)?;
                                }
                                write!(esp_data_file_buf, "{}{}{}{}", n, r, g, b)?;
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
            manager.udp_socket = Some(
                UdpSocket::bind(format!("{}:{}", manager.host, manager.port)).unwrap_or_else(
                    |_| {
                        panic!(
                            "Could not establish UDP connection to {}:{}!",
                            manager.host, manager.port
                        )
                    },
                ),
            );
        }
        match manager.udp_socket.as_mut() {
            Some(udp_socket) => {
                udp_socket
                    .set_read_timeout(Some(Duration::new(0, manager.udp_read_timeout * 1000000)))
                    .expect("set_read_timeout call failed");

                let bytes: [u8; 4] = [n, r, g, b];
                udp_socket.send(&bytes).unwrap_or_else(|_| {
                    panic!(
                        "Could not send packet: {:#?} to {}:{}!",
                        bytes, manager.host, manager.port
                    )
                });
                let mut buf = [0; 3];
                let udp_result = udp_socket.recv(&mut buf);

                match udp_result {
                    // TODO: This is untested! Test it
                    Ok(_size) => {}
                    Err(ref e) if e.kind() == WouldBlock => {
                        warn!(
                            "UDP timeout reached! Will resend packet, but won't wait for response!"
                        );
                        udp_socket.send(&bytes).unwrap_or_else(|_| {
                            panic!(
                                "Could not send packet: {:#?} to {}:{}!",
                                bytes, manager.host, manager.port
                            )
                        });
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
    }
    Ok(())
}

fn check_and_create_file(file: &PathBuf) -> Result<File, Box<dyn Error>> {
    if !Path::new(&file).exists() {
        Path::new(&file);
    } else {
        let remove_file_result = remove_file(file);
        match remove_file_result {
            Ok(()) => debug!("Removed {}", &file.display()),
            Err(error) => error!("Could not remove {}: {}.", &file.display(), error),
        }
    }
    let data_file = File::open(file.clone())
        .unwrap_or_else(|_| panic!("Could not open {} for writing!", file.display()));

    Ok(data_file)
}
