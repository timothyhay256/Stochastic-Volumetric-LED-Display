use std::{
    error::Error,
    fs::{remove_file, File},
    io::{BufWriter, Read, Write},
    net::{Ipv4Addr, UdpSocket},
    path::{Path, PathBuf},
    sync::{Arc, Mutex},
    time::SystemTime,
};

use crossbeam_channel::Sender;
use log::{debug, error, info, warn}; // TODO: Depreceate unity export byte data
use opencv::prelude::*;
use serde::Deserialize;
use serialport::SerialPort;

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
    pub record_data: bool,
    pub record_esp_data: bool,
    pub unity_controls_recording: bool,
    pub record_data_file: PathBuf,
    pub record_esp_data_file: PathBuf,
    pub multi_camera: bool,
    pub camera_index_1: i32,
    pub camera_index_2: Option<i32>,
    pub unity_options: UnityOptions,
    pub scan_mode: u32, // 0 is default, 1 filters by color first (Useful when you aren't scanning in perfect conditions)
    pub filter_color: Option<u32>, // 0 for red, 1 for green, 2 for blue
    pub filter_range: Option<u8>, // Range for color filter
    pub color_bright: Option<u8>,
    pub video_width: Option<f64>,
    pub video_height: Option<f64>,
    pub advanced: AdvancedConfig,
}

#[derive(Deserialize, Debug, Clone)]
pub struct AdvancedConfig {
    pub serial_read_timeout: Option<u32>,
    pub print_send_back: Option<bool>,
    pub con_fail_limit: Option<u32>,
    pub udp_read_timeout: u32,
    pub no_controller: Option<bool>,
    pub hsv_red_override: Option<Vec<u8>>, // Override the filter band for the red color when using a color filter. Should be formatted like <upper_h, upper_s, upper_v, lower_h, lower_s, lower_v>
    pub hsv_green_override: Option<Vec<u8>>,
    pub hsv_blue_override: Option<Vec<u8>>,
    pub no_video: Option<bool>, // When set in conjunction with streamlined being true, no video feed will show.
    pub get_events_streams_video: Option<bool>, // When set to true, get_events will stream to frame_cam_1/2
    pub get_events_video_widgets: Option<bool>, // When set to true, get_events video stream will include circles around illuminated LEDs for visualization purposes
    pub get_events_widgets_pos_index: Option<i32>, // Which pos file to use for visualization
    pub use_queue: Option<bool>,                // Should set_color queue writes?
    pub queue_size: Option<usize>,
    pub skip_confirmation: Option<bool>, // Should we skip checking if the LED was properly set? Speeds things way up at the cost of accuracy.
    pub crop_override: Option<Vec<i32>>, // When set, cropping will be skipped.
    pub demo_options: Option<DemoConfig>,
}

#[derive(Deserialize, Debug, Clone)]
pub struct DemoConfig {
    // Used for gyroscope demo
    pub gyro_port: i32,
}

#[derive(Deserialize, Clone, Debug)]
pub struct UnityOptions {
    pub num_container: u8,
    pub unity_ip: Ipv4Addr,
    pub unity_ports: Vec<u32>,
    pub unity_position_files: Vec<PathBuf>,
    pub scale: f32,
}
#[derive(Debug)]
pub struct ManagerData {
    pub config: RuntimeConfig,
    pub state: ManagerState,
    pub io: IOHandles,
    pub vision: VisionData,
}

#[derive(Debug, Clone)]
pub struct RuntimeConfig {
    pub num_led: u32,
    pub num_strips: u32,
    pub communication_mode: i8,
    pub host: Ipv4Addr,
    pub port: i32,
    pub serial_port_paths: Vec<String>,
    pub baud_rate: u32,
    pub serial_read_timeout: Option<u32>,
    pub record_data: bool,
    pub record_esp_data: bool,
    pub unity_controls_recording: bool,
    pub record_data_file: PathBuf,
    pub record_esp_data_file: PathBuf,
    pub print_send_back: Option<bool>,
    pub udp_read_timeout: u32,
    pub con_fail_limit: Option<u32>,
    pub no_controller: Option<bool>,
    pub scan_mode: u32,
    pub filter_color: Option<u32>,
    pub filter_range: Option<u8>,
    pub hsv_red_override: Option<Vec<u8>>,
    pub hsv_green_override: Option<Vec<u8>>,
    pub hsv_blue_override: Option<Vec<u8>>,
    pub no_video: Option<bool>,
    pub skip_confirmation: Option<bool>,
    pub use_queue: Option<bool>,
    pub queue_size: Option<usize>,
    pub led_config: Option<LedConfig>, // Exists so that we don't have to create a new struct every time we call set_color. Acts just as a holder for other items from RuntimeConfig
}

#[derive(Debug)]
pub struct ManagerState {
    pub first_run: bool,
    pub call_time: SystemTime,
    pub keepalive: bool,
    pub led_state: LedState,
    pub led_thread_channels: Vec<Sender<Task>>,
}

#[derive(Debug)]
pub struct IOHandles {
    pub udp_socket: Option<UdpSocket>,
    pub serial_port: Vec<Box<dyn SerialPort>>,
    pub data_file_buf: Option<BufWriter<File>>,
    pub esp_data_file_buf: Option<BufWriter<File>>,
}

#[derive(Debug)]
pub struct VisionData {
    pub frame_cam_1: Mat,
    pub frame_cam_2: Mat,
}

#[derive(Clone)]
pub struct GetEventsFrameBuffer {
    pub shared_frame_1: Mat,
    pub shared_frame_2: Mat,
}

#[derive(Clone, Debug)]
pub struct ScanData {
    pub pos: CropPos,
    pub invert: bool,
    pub depth: bool,
}

#[derive(Clone, Debug)]
pub struct CropPos {
    pub x1_start: i32,
    pub y1_start: i32,
    pub x1_end: i32,
    pub y1_end: i32,
    pub x2_start: Option<i32>,
    pub y2_start: Option<i32>,
    pub x2_end: Option<i32>,
    pub y2_end: Option<i32>,
    pub cam_1_brightest: Option<f64>,
    pub cam_2_brightest: Option<f64>,
    pub cam_1_darkest: Option<f64>,
    pub cam_2_darkest: Option<f64>,
}

#[derive(Debug)]
pub struct LedState {
    pub failures: u32,
    pub queue_lengths: Vec<u8>,
}

#[derive(Debug, Clone)]
pub struct LedConfig {
    // This contains values that will be cloned before moving into closure inside a thread so we don't have to deal with shared configs when using queues inside led_manager.
    pub skip_confirmation: Option<bool>,
    pub unity_controls_recording: bool,
    pub no_controller: Option<bool>,
    pub port: i32,
    pub communication_mode: i8,
    pub num_led: u32,
    pub num_strips: u32,
    pub serial_read_timeout: Option<u32>,
    pub udp_read_timeout: u32,
    pub host: Ipv4Addr,
    pub con_fail_limit: Option<u32>,
    pub print_send_back: Option<bool>,
    pub serial_port_paths: Vec<String>,
}

#[derive(Copy, Clone)]
pub struct Task {
    pub command: (u16, u8, u8, u8),
    pub controller_queue_length: Option<u8>,
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
    let serial_read_timeout = config_holder.advanced.serial_read_timeout;

    let record_data = config_holder.record_data;
    let record_esp_data = config_holder.record_esp_data;
    let unity_controls_recording = config_holder.unity_controls_recording;
    let record_data_file = config_holder.record_data_file.clone();
    let record_esp_data_file = config_holder.record_esp_data_file.clone();
    let udp_read_timeout = config_holder.advanced.udp_read_timeout;
    let con_fail_limit = config_holder.advanced.con_fail_limit;

    let multi_camera = config_holder.multi_camera;

    let print_send_back = config_holder.advanced.print_send_back;

    let no_controller = config_holder.advanced.no_controller;

    let scan_mode = config_holder.scan_mode;
    let filter_color = config_holder.filter_color;
    let filter_range = config_holder.filter_range;

    let hsv_red_override = config_holder.advanced.hsv_red_override.clone();
    let hsv_green_override = config_holder.advanced.hsv_green_override.clone();
    let hsv_blue_override = config_holder.advanced.hsv_blue_override.clone();

    let no_video = config_holder.advanced.no_video;

    let skip_confirmation = config_holder.advanced.skip_confirmation;

    let use_queue = config_holder.advanced.use_queue;
    let queue_size = config_holder.advanced.queue_size;

    // Validate config and inform user of settings

    if let Some(no_controller) = no_controller {
        if !no_controller {
            if communication_mode == 2 {
                for path in serial_port_paths.iter() {
                    if Path::new(&path).exists() {
                        info!("Using serial for communication on {path}!");
                    } else {
                        panic!("Serial port {path} does not exist!");
                    }
                }
            } else if communication_mode == 1 {
                info!("Using udp for communication at {host} on port {port}");
            }
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
            config: RuntimeConfig {
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
                print_send_back,
                udp_read_timeout,
                con_fail_limit,
                no_controller,
                scan_mode,
                filter_color,
                filter_range,
                hsv_red_override: hsv_red_override.clone(),
                hsv_green_override: hsv_green_override.clone(),
                hsv_blue_override: hsv_blue_override.clone(),
                no_video,
                skip_confirmation,
                use_queue,
                queue_size,
                led_config: None,
            },
            state: ManagerState {
                first_run: true,
                call_time: SystemTime::now(),
                keepalive: true,
                led_state: {
                    LedState {
                        failures: 0,
                        queue_lengths: Vec::new(),
                    }
                },
                led_thread_channels: Vec::new(),
            },
            io: IOHandles {
                data_file_buf: None,
                esp_data_file_buf: None,
                udp_socket: None,
                serial_port: Vec::new(),
            },
            vision: VisionData {
                frame_cam_1: Default::default(),
                frame_cam_2: Default::default(),
            },
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
    if manager.io.data_file_buf.is_some() {
        if let Some(data_file_buf) = manager.io.data_file_buf.as_mut() {
            match data_file_buf.flush() {
                Ok(_) => {}
                Err(e) => {
                    error!(
                        "Could not flush {}! It may be incomplete or corrupted. Error: {}",
                        manager.config.record_data_file.display(),
                        e
                    )
                }
            }
        };
    }

    if manager.io.esp_data_file_buf.is_some() {
        if let Some(esp_data_file_buf) = manager.io.esp_data_file_buf.as_mut() {
            match esp_data_file_buf.flush() {
                Ok(_) => {}
                Err(e) => {
                    error!(
                        "Could not flush {}! It may be incomplete or corrupted. Error: {}",
                        manager.config.record_esp_data_file.display(),
                        e
                    )
                }
            }
        };
    }
}
