use log::{debug, error, info, warn}; // TODO: Depreceate unity export byte data
use serde::Deserialize;
use serialport::SerialPort;
use std::{
    error::Error,
    fs::{remove_file, File},
    io::{BufWriter, Read, Write},
    net::{Ipv4Addr, UdpSocket},
    path::{Path, PathBuf},
    sync::{Arc, Mutex},
    time::SystemTime,
};

#[derive(Deserialize, Debug, Clone)]
pub struct Config {
    // TODO: All of these should also be passable via commandline
    pub num_led: u32,
    pub num_strips: u32,
    pub communication_mode: i8,
    pub host: Ipv4Addr,
    pub port: i32,
    pub serial_port_paths: Vec<String>,
    pub baud_rate: u32,
    pub serial_read_timeout: u32,
    pub record_data: bool,
    pub record_esp_data: bool,
    pub unity_controls_recording: bool,
    pub record_data_file: PathBuf,
    pub record_esp_data_file: PathBuf,
    pub print_send_back: bool,
    pub udp_read_timeout: u32,
    pub multi_camera: bool,
    pub camera_index_1: i32,
    pub camera_index_2: Option<i32>,
    pub con_fail_limit: u32,
    pub no_controller: bool,
    pub unity_options: UnityOptions,
}

#[derive(Deserialize, Clone, Debug)]
pub struct UnityOptions {
    pub num_container: u8,
    pub unity_ip: Ipv4Addr,
    pub unity_ports: Vec<u32>,
    pub unity_serial_ports: Vec<PathBuf>,
    pub unity_position_files: Vec<PathBuf>,
    pub scale: f32,
}
#[derive(Debug)]
pub struct ManagerData {
    // Used to persist data through led_manager::set_color.
    pub num_led: u32,
    pub num_strips: u32,
    pub communication_mode: i8,
    pub host: Ipv4Addr,
    pub port: i32,
    pub serial_port_paths: Vec<String>,
    pub baud_rate: u32,
    pub serial_read_timeout: u32,
    pub record_data: bool,
    pub record_esp_data: bool,
    pub unity_controls_recording: bool,
    pub record_data_file: PathBuf,
    pub record_esp_data_file: PathBuf,
    pub print_send_back: bool,
    pub udp_read_timeout: u32,
    pub failures: u32,
    pub con_fail_limit: u32,
    pub first_run: bool, // First ManagerData specific option, above is just Config
    pub call_time: SystemTime, // Used to persist so we can track time between set_color calls
    pub data_file_buf: Option<BufWriter<File>>, // On the first run that requires writing to disk, this will be initialized.
    pub esp_data_file_buf: Option<BufWriter<File>>, // We could either add two new variables to track each ones init state, or we could just init both when either one needs to.
    pub udp_socket: Option<UdpSocket>, // The second option reduces clutter, and barely reduces performance, so we do that.
    pub serial_port: Vec<Box<dyn SerialPort>>,
    pub keepalive: bool, // Should our threads stay alive?
    pub queue_lengths: Vec<u8>,
    pub no_controller: bool, // For debugging. Should the set_color function do everything EXCEPT actually attempt to set the color?
}

pub fn load_validate_conf(config_path: &Path) -> (ManagerData, UnityOptions, Config) {
    // Load and validate config
    if !config_path.exists() {
        panic!("Could not find svled.toml! Please create one according to the documentation in the current directory.");
    }
    let mut config_file =
        File::open(config_path).expect("Could not open config file. Do I have permission?");
    let mut config_file_contents = String::new();
    config_file
        .read_to_string(&mut config_file_contents)
        .expect(
            "The config file contains non UTF-8 characters, what in the world did you put in it??",
        );
    let config_holder: Config = toml::from_str(&config_file_contents)
        .expect("The config file was not formatted properly and could not be read.");

    let num_led = config_holder.num_led;
    let num_strips = config_holder.num_strips;
    let communication_mode = config_holder.communication_mode;
    let host = config_holder.host;
    let port = config_holder.port;
    let serial_port_paths = config_holder.serial_port_paths.clone();
    let baud_rate = config_holder.baud_rate;
    let serial_read_timeout = config_holder.serial_read_timeout;

    let record_data = config_holder.record_data;
    let record_esp_data = config_holder.record_esp_data;
    let unity_controls_recording = config_holder.unity_controls_recording;
    let record_data_file = config_holder.record_data_file.clone();
    let record_esp_data_file = config_holder.record_esp_data_file.clone();
    let udp_read_timeout = config_holder.udp_read_timeout;
    let con_fail_limit = config_holder.con_fail_limit;

    let multi_camera = config_holder.multi_camera;

    let print_send_back = config_holder.print_send_back;

    let no_controller = config_holder.no_controller;

    // Validate config and inform user of settings

    if !no_controller {
        if communication_mode == 2 {
            for path in serial_port_paths.iter() {
                if Path::new(path).exists() {
                    info!("Using serial for communication on {}!", path);
                } else {
                    panic!("Serial port {} does not exist!", path);
                }
            }
        } else if communication_mode == 1 {
            info!("Using udp for communication at {} on port {}", host, port);
        }
    }

    if unity_controls_recording || record_data || record_esp_data {
        if Path::new(&record_data_file).exists() && record_data {
            warn!(
                "{} will be overwritten once you start recording!",
                record_data_file.display()
            );
        }
        if Path::new(&record_esp_data_file).exists() && record_esp_data {
            warn!(
                "{} will be overwritten once you start recording!",
                record_esp_data_file.display()
            )
        }
    }
    if unity_controls_recording {
        info!("Unity will control recording of data.");
    } else if record_data {
        info!(
            "All data will be recorded during this session in VLED format to {}",
            record_data_file.display()
        );
    } else if record_esp_data {
        info!(
            "All data will be recorded during this session in bVLED format to {}",
            record_esp_data_file.display()
        );
    } else {
        warn!("No bVLED or VLED data will be recorded!");
    }

    if multi_camera {
        info!("Using multiple cameras!");
    }

    (
        ManagerData {
            num_led,
            num_strips,
            communication_mode,
            host,
            port,
            serial_port_paths: serial_port_paths.clone(), // So we can create new ManagerDatas
            baud_rate,
            serial_read_timeout,
            record_data,
            record_data_file: record_data_file.clone(),
            record_esp_data,
            unity_controls_recording,
            record_esp_data_file: record_esp_data_file.clone(),
            failures: 0,
            con_fail_limit,
            print_send_back,
            udp_read_timeout,
            first_run: true,
            call_time: SystemTime::now(),
            data_file_buf: None,
            esp_data_file_buf: None,
            udp_socket: None,
            serial_port: Vec::new(),
            keepalive: true,
            queue_lengths: Vec::new(),
            no_controller,
        },
        config_holder.unity_options.clone(),
        config_holder,
    )
}

pub fn check_and_create_file(file: &PathBuf) -> Result<File, Box<dyn Error>> {
    if Path::new(&file).exists() {
        let remove_file_result = remove_file(file);
        match remove_file_result {
            Ok(()) => debug!("Removed {}", &file.display()),
            Err(error) => error!("Could not remove {}: {}.", &file.display(), error),
        }
        match File::create(file) {
            Ok(_) => {}
            Err(e) => {
                panic!("Could not create {}: {}", file.display(), e);
            }
        }
    }
    let data_file = match File::create(file.clone()) {
        Ok(file) => file,
        Err(e) => panic!("Could not open {}: {}", file.display(), e),
    };

    Ok(data_file)
}

pub fn flush_data(manager_guard: Arc<Mutex<ManagerData>>) {
    let mut manager = manager_guard.lock().unwrap();
    // Flush our BufWriters
    if manager.data_file_buf.is_some() {
        if let Some(data_file_buf) = manager.data_file_buf.as_mut() {
            match data_file_buf.flush() {
                Ok(_) => {}
                Err(e) => {
                    error!(
                        "Could not flush {}! It may be incomplete or corrupted. Error: {}",
                        manager.record_data_file.display(),
                        e
                    )
                }
            }
        };
    }

    if manager.esp_data_file_buf.is_some() {
        if let Some(esp_data_file_buf) = manager.esp_data_file_buf.as_mut() {
            match esp_data_file_buf.flush() {
                Ok(_) => {}
                Err(e) => {
                    error!(
                        "Could not flush {}! It may be incomplete or corrupted. Error: {}",
                        manager.record_esp_data_file.display(),
                        e
                    )
                }
            }
        };
    }
}
