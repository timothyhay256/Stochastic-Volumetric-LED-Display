use log::info;
use rand::Rng;
use std::{
    sync::{Arc, Mutex},
    time::Instant,
};

use crate::led_manager;
use crate::ManagerData;

pub fn speedtest(manager: &Arc<Mutex<ManagerData>>, num_led: u32, writes: u32) {
    let mut rng = rand::thread_rng();
    info!("Clearing string");

    for n in 0..num_led {
        led_manager::set_color(manager, n as u16, 0, 0, 0);
    }

    info!("Testing {} random writes", writes);
    let start = Instant::now();

    for _n in 0..=writes {
        led_manager::set_color(
            manager,
            rng.gen_range(0..(num_led as u16) - 1),
            rng.gen_range(0..255),
            rng.gen_range(0..255),
            rng.gen_range(0..255),
        );
    }

    let queue_lengths = manager.lock().unwrap().state.queue_lengths.clone();

    let end = start.elapsed();

    let mut queue_total_lengths: u32 = 0;
    for n in queue_lengths.iter().take((queue_lengths.len() - 1) + 1) {
        queue_total_lengths += queue_lengths[*n as usize] as u32;
    }

    info!("{:.2?} seconds.", end);
    info!("{:.5?} seconds per LED", end / writes);
    info!(
        "{:.3} LEDs per second",
        (writes as f64 / (end.as_millis() as f64)) * 1000.0
    );
    info!(
        "Average queue length: {}",
        queue_total_lengths / queue_lengths.len() as u32
    );
}
