use log::info;
use rand::Rng;
use std::time::Instant;

use crate::led_manager;
use crate::ManagerData;

pub fn speedtest(manager: &mut ManagerData, num_led: u32, writes: u32) {
    let mut rng = rand::thread_rng();
    info!("Clearing string");

    for n in 0..=num_led {
        led_manager::set_color(manager, n as u8, 0, 0, 0);
    }

    info!("Testing {} random writes", writes);
    let start = Instant::now();

    for _n in 0..=writes {
        led_manager::set_color(
            manager,
            rng.gen_range(0..num_led.try_into().unwrap()),
            rng.gen_range(0..255),
            rng.gen_range(0..255),
            rng.gen_range(0..255),
        );
    }

    let end = start.elapsed();

    info!("{:.2?} seconds.", end);
    info!("{:.5?} seconds per LED", end / writes);
    info!(
        "{:.3} LEDs per second",
        (writes as f64 / (end.as_millis() as f64)) * 1000.0
    );
}
