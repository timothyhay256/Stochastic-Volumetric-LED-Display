use std::{
    sync::{Arc, Mutex},
    thread::sleep,
    time::Duration,
};

use crate::{led_manager, ManagerData};
type JsonEntry = Vec<(String, (f32, f32), (f32, f32))>;
type LedPos = [(String, (f32, f32), (f32, f32))];

#[derive(Clone, Copy)]
pub enum Axis {
    X,
    Y,
    Z,
}

pub fn rainbow(
    manager: &Arc<Mutex<ManagerData>>,
    led_pos: &JsonEntry,
    step: f32,
    fuzz: f32,
    flip: bool,
    axis: Axis,
    clear: bool,
) {
    // Axis value accessor closure
    let get_axis_value = |entry: &(String, (f32, f32), (f32, f32))| -> f32 {
        match axis {
            Axis::X => entry.1 .0,
            Axis::Y => entry.1 .1,
            Axis::Z => entry.2 .0,
        }
    };

    // Determine highest and lowest bounds for the axis
    let mut highest = f32::MIN;
    let mut lowest = f32::MAX;

    for entry in led_pos.iter().take(led_pos.len().saturating_sub(1)) {
        let val = get_axis_value(entry);
        if val > highest {
            highest = val;
        }
        if val < lowest {
            lowest = val;
        }
    }

    let (mut j, end, z) = if flip {
        (highest - step, lowest, -step)
    } else {
        (lowest, highest, step)
    };

    // === RAINBOW FILL SWEEP ===
    while if z > 0.0 { j < end } else { j > end } {
        for (i, entry) in led_pos.iter().enumerate() {
            let val = get_axis_value(entry);
            if (j - fuzz) <= val && val <= (j + fuzz) {
                let hue_pos = (val - lowest) / (highest - lowest);
                let (r, g, b) = hsv_to_rgb(hue_pos, 1.0, 1.0);
                led_manager::set_color(manager, i.try_into().unwrap(), r, g, b);
            }
        }

        sleep(Duration::from_millis(20));
        j += z;
    }

    // === SWEEP CLEAR IN SAME DIRECTION ===
    if clear {
        let (mut j_clear, end_clear, z_clear) = if flip {
            (highest - step, lowest, -step)
        } else {
            (lowest, highest, step)
        };

        while if z_clear > 0.0 {
            j_clear < end_clear
        } else {
            j_clear > end_clear
        } {
            for (i, entry) in led_pos.iter().enumerate() {
                let val = get_axis_value(entry);
                if (j_clear - fuzz) <= val && val <= (j_clear + fuzz) {
                    led_manager::set_color(manager, i.try_into().unwrap(), 0, 0, 0);
                }
            }

            sleep(Duration::from_millis(20));
            j_clear += z_clear;
        }
    }
}

pub fn rainbow_fill(manager: &Arc<Mutex<ManagerData>>, led_pos: &LedPos, axis: Axis, offset: f32) {
    let get_axis_value = |entry: &(String, (f32, f32), (f32, f32))| -> f32 {
        match axis {
            Axis::X => entry.1 .0,
            Axis::Y => entry.1 .1,
            Axis::Z => entry.2 .0,
        }
    };

    let mut highest = f32::MIN;
    let mut lowest = f32::MAX;

    for entry in led_pos.iter().take(led_pos.len().saturating_sub(1)) {
        let val = get_axis_value(entry);
        if val > highest {
            highest = val;
        }
        if val < lowest {
            lowest = val;
        }
    }

    let range = highest - lowest;

    for (i, entry) in led_pos.iter().enumerate() {
        let val = get_axis_value(entry);
        let mut hue_pos = (val - lowest) / range + offset;
        hue_pos = hue_pos.fract(); // Ensure it's within [0.0, 1.0)
        let (r, g, b) = hsv_to_rgb(hue_pos, 1.0, 1.0);
        led_manager::set_color(manager, i.try_into().unwrap(), r, g, b);
    }
}

fn hsv_to_rgb(h: f32, s: f32, v: f32) -> (u8, u8, u8) {
    let i = (h * 6.0).floor();
    let f = h * 6.0 - i;
    let p = v * (1.0 - s);
    let q = v * (1.0 - f * s);
    let t = v * (1.0 - (1.0 - f) * s);
    let (r, g, b) = match i as u32 % 6 {
        0 => (v, t, p),
        1 => (q, v, p),
        2 => (p, v, t),
        3 => (p, q, v),
        4 => (t, p, v),
        5 => (v, p, q),
        _ => (0.0, 0.0, 0.0),
    };
    ((r * 255.0) as u8, (g * 255.0) as u8, (b * 255.0) as u8)
}
