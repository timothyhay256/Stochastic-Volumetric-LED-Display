// Prevent console window in addition to Slint window in Windows release builds when, e.g., starting the app via file manager. Ignored on other platforms.
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use crate::utils::Config;
use crate::utils::ManagerData;
use std::error::Error;
use std::sync::{Arc, Mutex};
use std::time::SystemTime;

slint::include_modules!();

pub fn main(config: Config) -> Result<(), Box<dyn Error>> {
    let ui = AppWindow::new()?;

    // let shared_manager = Arc::new(Mutex::new(ManagerData {
    //     num_led: config.num_led,
    //     num_strips: config.num_strips,
    //     communication_mode: config.communication_mode,
    //     host: config.host,
    //     port: config.port,
    //     serial_port_paths: config.serial_port_paths.clone(),
    //     baud_rate: config.baud_rate,
    //     serial_read_timeout: config.serial_read_timeout,
    //     record_data: config.record_data,
    //     record_data_file: config.record_data_file.clone(),
    //     record_esp_data: config.record_esp_data,
    //     unity_controls_recording: config.unity_controls_recording,
    //     record_esp_data_file: config.record_esp_data_file.clone(),
    //     failures: 0,
    //     con_fail_limit: config.con_fail_limit,
    //     print_send_back: config.print_send_back,
    //     udp_read_timeout: config.udp_read_timeout,
    //     first_run: true,
    //     call_time: SystemTime::now(),
    //     data_file_buf: None,
    //     esp_data_file_buf: None,
    //     udp_socket: None,
    //     serial_port: Vec::new(),
    //     keepalive: true,
    // }));

    // let shared_manager_speedtest = Arc::clone(&shared_manager);
    // ui.on_speedtest(move || {
    //     thread::spawn(move || {
    //         let mut manager = shared_manager_speedtest.lock().unwrap();
    //         speedtest(&mut manager, config.num_led, 750);
    //     });
    // });

    // let shared_manager_calibrate = Arc::clone(&shared_manager);
    // ui.on_calibrate(move || {
    //     thread::spawn(move || {
    //         let mut manager = shared_manager_calibrate.lock().unwrap();
    //         scan::scan(config.clone(), &mut manager).unwrap();
    //     })
    // });

    // ui.run()?;

    Ok(())
}
