use log::info;
use std::fs::File;
use std::io::prelude::*;
use std::net::TcpStream;

use crate::led_manager;
use crate::UnityOptions;

fn send_pos(unity: UnityOptions) -> std::io::Result<()> {
    for i in 0..=unity.num_container {
        let pos_file = match File::open(unity.unity_position_files[i as usize].clone()) {
            Ok(file) => file,
            Err(e) => {
                panic!(
                    "Could not read {:?}: {}",
                    unity.unity_position_files[i as usize], e
                )
            }
        };

        let mut stream = TcpStream::connect(format!(
            "{}:{}",
            unity.unity_ip, unity.unity_ports[i as usize]
        ))?;

        stream.write_all(&[1])?;
        stream.read_exact(&mut [0; 128])?;
    }
    Ok(())
}
