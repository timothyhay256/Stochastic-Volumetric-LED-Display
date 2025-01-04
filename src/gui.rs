// Prevent console window in addition to Slint window in Windows release builds when, e.g., starting the app via file manager. Ignored on other platforms.
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use crate::scan;
use crate::speedtest;
use crate::speedtest::speedtest;
use crate::Config;
use crate::ManagerData;
use std::error::Error;

slint::include_modules!();

pub fn main(config: Config, mut manager: ManagerData) -> Result<(), Box<dyn Error>> {
    let ui = AppWindow::new()?;

    // ui.on_request_increase_value({
    //     let ui_handle = ui.as_weak();
    //     move || {
    //         let ui = ui_handle.unwrap();
    //         ui.set_counter(ui.get_counter() + 1);
    //     }
    // });
    ui.on_speedtest(move || {
        speedtest(&mut manager, config.num_led, 750);
    });

    ui.on_calibrate(move || {
        scan::scan(config, &mut manager).unwrap();
    });

    ui.run()?;

    Ok(())
}
