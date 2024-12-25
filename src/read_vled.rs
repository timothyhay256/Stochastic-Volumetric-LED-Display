use log::info;
use std::error::Error;
use std::fs::File;
use std::io::{self, BufRead};
use std::path::Path;
use std::path::PathBuf;
use std::time::Instant;
use std::{thread, time};

use crate::led_manager;
use crate::ManagerData;

pub fn read_vled(manager: &mut ManagerData, file: PathBuf) -> Result<(), Box<dyn Error>> {
    info!("Playing back {}", file.display());

    let mut start = Instant::now();
    let mut packets_per_second = 0;
    if let Ok(lines) = read_lines(file) {
        // Consumes the iterator, returns an (Optional) String
        for mut line in lines.map_while(Result::ok) {
            if line.contains("E") {
                // Clear color of index `EN`
                let index = match line.remove(1).to_string().parse::<u8>() {
                    Ok(index) => index,
                    Err(e) => {
                        panic!(
                            "VLED was malformed: Attempted to convert {} to u8: {}",
                            line.remove(1),
                            e
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
                let sleep = match line.remove(1).to_string().parse::<u8>() {
                    Ok(sleep) => sleep,
                    Err(e) => {
                        panic!(
                            "VLED was malformed: Attempted to convert {} to u8: {}",
                            line.remove(1),
                            e
                        )
                    }
                };
                thread::sleep(time::Duration::from_secs(sleep as u64));
            }

            if start.elapsed().as_secs() >= 1 {
                info!(
                    "{} packets per {} seconds.",
                    packets_per_second,
                    start.elapsed().as_millis() * 1000
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
