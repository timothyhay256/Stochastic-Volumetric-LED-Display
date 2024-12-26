use log::error;
use std::error::Error;
use std::fs::File;
use std::io::prelude::*;
use std::net::TcpStream;
use std::str;

use crate::UnityOptions;

pub fn send_pos(unity: UnityOptions) -> std::io::Result<()> {
    type JsonEntry = Vec<(String, (f32, f32), (f32, f32))>;
    for mut i in 1..=unity.num_container {
        i -= 1; // TODO: There is def a better way
        let mut pos_file = match File::open(unity.unity_position_files[i as usize].clone()) {
            Ok(file) => file,
            Err(e) => {
                panic!(
                    "Could not read {:?}: {}",
                    unity.unity_position_files[i as usize], e
                )
            }
        };

        let mut file_contents = String::new();
        match pos_file.read_to_string(&mut file_contents) {
            Ok(_) => {}
            Err(e) => {
                panic!(
                    "Could not read position file {}: {}",
                    unity.unity_position_files[i as usize].display(),
                    e
                )
            }
        };

        let json: JsonEntry = match serde_json::from_str(&file_contents) {
            Ok(json) => json,
            Err(e) => {
                panic!(
                    "{} contains invalid or incomplete calibration data: {}",
                    unity.unity_position_files[i as usize].display(),
                    e
                )
            }
        };
        let mut stream = TcpStream::connect(format!(
            "{}:{}",
            unity.unity_ip, unity.unity_ports[i as usize]
        ))?;
        println!("how");
        for led in json.iter() {
            println!("fucky wucky");
            stream.write_all(
                format!(
                    "{},{},{}",
                    led.1 .0 * unity.scale,
                    led.1 .1 * unity.scale,
                    led.2 .0 * unity.scale
                )
                .as_bytes(),
            )?;
            let mut response: [u8; 3] = [0; 3];
            stream.read_exact(&mut response)?;

            if match str::from_utf8(&response) {
                Ok(v) => v,
                Err(e) => panic!("Invalid UTF-8 sequence: {}", e),
            } != "ack"
            {
                error!("Did not get acknowledgement from Unity! You may have missing LEDs.");
            }
        }

        stream.write_all("END".as_bytes())?;
    }
    Ok(())
}

pub fn get_events(unity: UnityOptions) -> Result<(), Box<dyn Error>> {
    Ok(())
}
