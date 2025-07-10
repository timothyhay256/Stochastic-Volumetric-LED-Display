use std::{
    fs,
    sync::{Arc, Mutex},
    thread::sleep,
    time::{Duration, Instant},
};

use image::{io::Reader as ImageReader, GenericImageView, Pixel};
use log::error;
use regex::Regex;

use crate::{led_manager, set_color, ManagerData, PosEntry};
type LedPos = [(String, (i32, i32), (i32, i32))];

#[derive(Clone, Copy)]
pub enum Axis {
    X,
    Y,
    Z,
}

pub fn rainbow(
    manager: &Arc<Mutex<ManagerData>>,
    led_pos: &PosEntry,
    step: i32,
    fuzz: i32,
    flip: bool,
    axis: Axis,
    clear: bool,
) {
    // Axis value accessor closure
    let get_axis_value = |entry: &(String, (i32, i32), (i32, i32))| -> i32 {
        match axis {
            Axis::X => entry.1 .0,
            Axis::Y => entry.1 .1,
            Axis::Z => entry.2 .0,
        }
    };

    // Determine highest and lowest bounds for the axis
    let mut highest = i32::MIN;
    let mut lowest = i32::MAX;

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
    while if z > 0 { j < end } else { j > end } {
        for (i, entry) in led_pos.iter().enumerate() {
            let val = get_axis_value(entry);
            if (j - fuzz) <= val && val <= (j + fuzz) {
                let hue_pos = (val - lowest) as f32 / (highest - lowest) as f32;
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

        while if z_clear > 0 {
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

pub fn rainbow_fill(manager: &Arc<Mutex<ManagerData>>, led_pos: &LedPos, axis: Axis, offset: i32) {
    let get_axis_value = |entry: &(String, (i32, i32), (i32, i32))| -> i32 {
        match axis {
            Axis::X => entry.1 .0,
            Axis::Y => entry.1 .1,
            Axis::Z => entry.2 .0,
        }
    };

    let mut highest = i32::MIN;
    let mut lowest = i32::MAX;

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
        let hue_pos = (val - lowest) as f32 / (range + offset) as f32;
        let (r, g, b) = hsv_to_rgb(hue_pos, 1.0, 1.0);
        led_manager::set_color(manager, i.try_into().unwrap(), r, g, b);
    }
}

fn hsv_to_rgb(h: f32, s: f32, v: f32) -> (u8, u8, u8) {
    let h = h.fract() * 6.0; // keep hue in [0,6)
    let i = h.floor() as u32;
    let f = h - h.floor();

    let p = v * (1.0 - s);
    let q = v * (1.0 - f * s);
    let t = v * (1.0 - (1.0 - f) * s);

    let (r, g, b) = match i {
        0 => (v, t, p),
        1 => (q, v, p),
        2 => (p, v, t),
        3 => (p, q, v),
        4 => (t, p, v),
        5 | _ => (v, p, q),
    };

    (
        (r * 255.0).round() as u8,
        (g * 255.0).round() as u8,
        (b * 255.0).round() as u8,
    )
}

pub fn render_jpg_onto_leds(
    image_path: &str,
    led_positions: &PosEntry,
    manager: &Arc<Mutex<ManagerData>>,
    z_range: Option<std::ops::RangeInclusive<i32>>, // ← now a range
) {
    // Load JPG image
    let img = match image::ImageReader::open(image_path) {
        Ok(reader) => match reader.decode() {
            Ok(decoded) => decoded,
            Err(e) => {
                error!("Failed to decode image: {e}");
                return;
            }
        },
        Err(e) => {
            error!("Failed to open image: {e}");
            return;
        }
    };

    let img_width = img.width() as f32;
    let img_height = img.height() as f32;

    // Filter positions based on z_range
    let filtered: Vec<(usize, &(_, (i32, i32), (i32, i32)))> = led_positions
        .iter()
        .enumerate()
        .filter(|(_, (_, _, (z, _)))| match &z_range {
            Some(range) => range.contains(z),
            None => true,
        })
        .collect();

    if filtered.is_empty() {
        error!("No LEDs matched z_range = {z_range:?}");
        return;
    }

    // Compute 2D bounds from filtered LEDs
    let (min_x, max_x, min_y, max_y) = filtered.iter().fold(
        (i32::MAX, i32::MIN, i32::MAX, i32::MIN),
        |(min_x, max_x, min_y, max_y), &(_, (_, (x, y), _))| {
            (min_x.min(*x), max_x.max(*x), min_y.min(*y), max_y.max(*y))
        },
    );

    let width_range = (max_x - min_x).max(1) as f32;
    let height_range = (max_y - min_y).max(1) as f32;

    for (i, (_name, (x, y), _z)) in filtered.into_iter() {
        let norm_x = ((*x - min_x) as f32 / width_range * (img_width - 1.0)).round() as u32;
        let norm_y = ((*y - min_y) as f32 / height_range * (img_height - 1.0)).round() as u32;

        if norm_x < img.width() && norm_y < img.height() {
            let pixel = img.get_pixel(norm_x, norm_y).to_rgb();
            let [r, g, b] = pixel.0;
            set_color(manager, i as u16, r, g, b);
        }
    }
}

// pub fn render_jpg_sequence(
//     dir_path: &str,
//     prefix: &str, // e.g. "somename_"
//     led_positions: &PosEntry,
//     manager: &Arc<Mutex<ManagerData>>,
//     z_range: Option<std::ops::RangeInclusive<i32>>,
// ) {
//     let pattern = format!(r"{}(\d+)\.jpg", regex::escape(prefix));
//     let re = Regex::new(&pattern).expect("invalid regex");

//     let mut image_files: Vec<(u32, String)> = fs::read_dir(dir_path)
//         .expect("Failed to read directory")
//         .filter_map(|entry| {
//             let path = entry.ok()?.path();
//             let fname = path.file_name()?.to_string_lossy();

//             re.captures(&fname).and_then(|caps| {
//                 caps.get(1).and_then(|num_match| {
//                     num_match
//                         .as_str()
//                         .parse::<u32>()
//                         .ok()
//                         .map(|n| (n, path.to_string_lossy().to_string()))
//                 })
//             })
//         })
//         .collect();

//     // Sort files by extracted number
//     image_files.sort_by_key(|&(n, _)| n);

//     if image_files.is_empty() {
//         error!("No matching images found in {}", dir_path);
//         return;
//     }

//     println!("Rendering {} frames from {}", image_files.len(), dir_path);

//     for (frame_num, file_path) in image_files {
//         render_jpg_onto_leds(&file_path, led_positions, manager, z_range.clone());
//         error!("Rendered frame {} from {}", frame_num, file_path);
//     }
// }
