use log::error;
use log::info;
use log::warn;
use std::error::Error;
use std::ffi::OsStr;
use std::fs::File;
use std::io::{self, BufRead};
use std::path::Path;
use std::path::PathBuf;
use std::time::Instant;
use std::{thread, time};

use crate::led_manager;
use crate::ManagerData;

pub fn read_vled(manager: &mut ManagerData, file: PathBuf) -> Result<(), Box<dyn Error>> {
    if file.extension().and_then(OsStr::to_str) != Some("vled") {
        // Only doing this check because I feel like people are gonna try using bvled files with this function
        warn!("File extension is not a vled file, it may not be read correctly");
    }
    info!("Playing back {}", file.display());

    let mut start = Instant::now();
    let mut packets_per_second = 0;
    if let Ok(lines) = read_lines(file) {
        // Consumes the iterator, returns an (Optional) String
        for mut line in lines.map_while(Result::ok) {
            if line.contains("E") {
                // Clear color of index `EN`
                line.remove(0);
                let index = match line.to_string().parse::<u8>() {
                    Ok(index) => index,
                    Err(e) => {
                        panic!(
                            "VLED was malformed: Attempted to convert {} to u8: {}",
                            line, e
                        )
                    }
                };
                led_manager::set_color(manager, index, 0, 0, 0);
                packets_per_second += 1;
            } else if line.contains("|") {
                // Set index n with r g b from string n|r|g|b
                let mut xs: [u8; 4] = [0; 4];
                let nrgb = line.split("|");
                for (i, el) in nrgb.enumerate() {
                    xs[i] = match el.to_string().parse::<u8>() {
                        Ok(el) => el,
                        Err(e) => {
                            panic!(
                                "VLED was malformed: Attempted to convert {} to u8: {}",
                                el, e
                            )
                        }
                    };
                }
                led_manager::set_color(manager, xs[0], xs[1], xs[2], xs[3]);
                packets_per_second += 1;
            } else if line.contains("T") {
                line.remove(0);
                line.remove(0);
                let sleep = match line.to_string().parse::<u8>() {
                    Ok(sleep) => sleep,
                    Err(e) => {
                        panic!(
                            "VLED was malformed: Attempted to convert {} to u8: {}",
                            line, e
                        )
                    }
                };
                // println!("Sleeping for {}");
                thread::sleep(time::Duration::from_millis(sleep as u64));
            } else {
                error!("Unable to parse invalid line of vled file.");
            }

            if start.elapsed().as_secs() >= 1 {
                info!(
                    "{} packets per {} seconds.",
                    packets_per_second,
                    start.elapsed().as_secs()
                );
                packets_per_second = 0;
                start = Instant::now();
            }
        }
    }

    Ok(())
}

fn read_lines<P>(filename: P) -> io::Result<io::Lines<io::BufReader<File>>>
where
    P: AsRef<Path>,
{
    let file = File::open(filename)?;
    Ok(io::BufReader::new(file).lines())
}
