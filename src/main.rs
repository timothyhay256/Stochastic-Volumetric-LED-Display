use gumdrop::Options;
use log::{debug, error, info, warn}; // TODO: Depreceate unity export byte data
use serde::Deserialize;
use serialport::SerialPort;
use std::error::Error;
use std::fs::{remove_file, File};
use std::process;
use std::thread;
use std::{
    env,
    io::{BufWriter, Read, Write},
    net::{Ipv4Addr, UdpSocket},
    path::{Path, PathBuf},
    time::SystemTime,
};

pub mod led_manager;
pub mod read_vled;
pub mod scan;
pub mod speedtest;
pub mod unity;

#[derive(Deserialize)]
pub struct Config {
    // TODO: All of these should also be passable via commandline
    num_led: u32,
    communication_mode: i8,
    host: Ipv4Addr,
    port: i32,
    serial_port_path: String,
    baud_rate: u32,
    serial_read_timeout: u32,
    record_data: bool,
    record_esp_data: bool,
    unity_controls_recording: bool,
    record_data_file: PathBuf,
    record_esp_data_file: PathBuf,
    print_send_back: bool,
    udp_read_timeout: u32,
    camera_index: i32,
    con_fail_limit: u32,
    unity_options: UnityOptions,
}

#[derive(Deserialize, Clone)]
pub struct UnityOptions {
    num_container: u8,
    unity_ip: Ipv4Addr,
    unity_ports: Vec<u32>,
    unity_serial_ports: Vec<PathBuf>,
    unity_position_files: Vec<PathBuf>,
    scale: f32,
}

pub struct ManagerData {
    // Used to persist data through led_manager::set_color.
    num_led: u32,
    communication_mode: i8,
    host: Ipv4Addr,
    port: i32,
    serial_port_path: String,
    baud_rate: u32,
    serial_read_timeout: u32,
    record_data: bool,
    record_esp_data: bool,
    unity_controls_recording: bool,
    record_data_file: PathBuf,
    record_esp_data_file: PathBuf,
    print_send_back: bool,
    udp_read_timeout: u32,
    failures: u32,
    con_fail_limit: u32,
    first_run: bool,       // First ManagerData specific option, above is just Config
    call_time: SystemTime, // Used to persist so we can track time between set_color calls
    data_file_buf: Option<BufWriter<File>>, // On the first run that requires writing to disk, this will be initialized.
    esp_data_file_buf: Option<BufWriter<File>>, // We could either add two new variables to track each ones init state, or we could just init both when either one needs to.
    udp_socket: Option<UdpSocket>, // The second option reduces clutter, and barely reduces performance, so we do that.
    serial_port: Option<Box<dyn SerialPort>>,
    keepalive: bool, // Should our threads stay alive?
}

#[derive(Debug, Options)]
struct MyOptions {
    #[options(help = "print help message")]
    help: bool,
    #[options(help = "be verbose")]
    verbose: bool,
    #[options(help = "specify a specific config file")]
    config: String,

    // The `command` option will delegate option parsing to the command type,
    // starting at the first free argument.
    #[options(command)]
    command: Option<Command>,
}

#[derive(Debug, Options)]
enum Command {
    // Command names are generated from variant names.
    // By default, a CamelCase name will be converted into a lowercase,
    // hyphen-separated name; e.g. `FooBar` becomes `foo-bar`.
    //
    // Names can be explicitly specified using `#[options(name = "...")]`
    #[options(help = "perform a connection speedtest")]
    Speedtest(SpeedtestOptions),

    #[options(help = "play back a vled file")]
    ReadVled(ReadvledOptions),

    #[options(help = "calibrate a svled container")]
    Calibrate(CalibrateOptions),

    #[options(help = "connect to Unity")]
    Unity(UnityCommandOptions),
}

#[derive(Debug, Options)]
struct SpeedtestOptions {}

#[derive(Debug, Options)]
struct ReadvledOptions {
    #[options(help = "vled file to read")]
    vled_file: PathBuf,
}

#[derive(Debug, Options)]
struct CalibrateOptions {}

#[derive(Debug, Options)]
struct UnityCommandOptions {}

fn main() {
    let opts = MyOptions::parse_args_default_or_exit();
    let mut config_path = Path::new("svled.toml");

    if opts.verbose {
        env::set_var("RUST_LOG", "debug");
    } else {
        env::set_var("RUST_LOG", "info");
    }

    env_logger::init();

    if !opts.config.is_empty() {
        info!("Using config {}", opts.config);
        config_path = Path::new(&opts.config);
    }

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
    let communication_mode = config_holder.communication_mode;
    let host = config_holder.host;
    let port = config_holder.port;
    let serial_port_path = config_holder.serial_port_path.clone();
    let baud_rate = config_holder.baud_rate;
    let serial_read_timeout = config_holder.serial_read_timeout;

    let record_data = config_holder.record_data;
    let record_esp_data = config_holder.record_esp_data;
    let unity_controls_recording = config_holder.unity_controls_recording;
    let record_data_file = config_holder.record_data_file.clone();
    let record_esp_data_file = config_holder.record_esp_data_file.clone();
    let udp_read_timeout = config_holder.udp_read_timeout;
    let con_fail_limit = config_holder.con_fail_limit;

    let print_send_back = config_holder.print_send_back;

    let unity_options = config_holder.unity_options.clone();

    // Validate config and inform user of settings

    if num_led > 255 {
        panic!("You currently cannot use over 255 LEDs do to how the driver delivers packets. This will be changed soon.")
    }
    if communication_mode == 2 {
        if Path::new(&serial_port_path).exists() {
            info!("Using serial for communication on {}!", serial_port_path);
        } else {
            panic!("Serial port {} does not exist!", serial_port_path);
        }
    } else if communication_mode == 1 {
        info!("Using udp for communication at {} on port {}", host, port);
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

    let mut manager = ManagerData {
        num_led,
        communication_mode,
        host,
        port,
        serial_port_path: serial_port_path.clone(), // So we can create new ManagerDatas
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
        serial_port: None,
        keepalive: true,
    };

    ctrlc::set_handler(move || {
        manager.keepalive = false;
        process::exit(0);
    })
    .expect("Error setting Ctrl-C handler");

    if let Some(Command::Speedtest(ref _speedtest_options)) = opts.command {
        info!("Performing speedtest...");

        speedtest::speedtest(&mut manager, num_led, 750);
    } else if let Some(Command::Calibrate(ref _calibrate_options)) = opts.command {
        info!("Performing calibrating");

        scan::scan(config_holder, &mut manager).expect("failure");
    } else if let Some(Command::ReadVled(ref readvled_options)) = opts.command {
        if !readvled_options.vled_file.is_file() {
            error!("You must pass a valid vled file with --vled-file!");
            process::exit(0);
        } else {
            info!("Playing back {}!", readvled_options.vled_file.display());

            manager.record_data = false;
            manager.record_esp_data = false;
            match read_vled::read_vled(&mut manager, readvled_options.vled_file.clone()) {
                Ok(_) => {}
                Err(e) => {
                    panic!(
                        "Could not read {}: {}",
                        readvled_options.vled_file.display(),
                        e
                    )
                }
            };
        }
    } else if let Some(Command::Unity(ref _unity_options)) = opts.command {
        // Validate Unity section of config, if we are using Unity.

        if unity_options.unity_serial_ports.len() < unity_options.num_container.into()
            || unity_options.unity_position_files.len() < unity_options.num_container.into()
        {
            panic!("You need to have enough paths in both unity_serial_ports and unity_position_files to continue!");
        }

        for i in 0..=unity_options.num_container - 1 {
            if !Path::new(&unity_options.unity_serial_ports[i as usize]).is_file() {
                error!(
                    "{} is not a valid file! Will attempt to continue anyway.",
                    unity_options.unity_serial_ports[i as usize]
                        .clone()
                        .display()
                );
            }
        }

        for i in 0..=unity_options.num_container - 1 {
            if !Path::new(&unity_options.unity_position_files[i as usize]).is_file() {
                error!(
                    "{} is not a valid file! Will attempt to continue anyway.",
                    unity_options.unity_position_files[i as usize]
                        .clone()
                        .display()
                );
            }
        }

        info!("Sending positions to Unity");

        match unity::send_pos(unity_options.clone()) {
            Ok(_) => {}
            Err(e) => {
                panic!("There was an issue connecting to Unity: {}", e);
            }
        };
        let mut children = Vec::new();

        info!("Spawning listening threads");

        for i in 0..unity_options.num_container {
            let mut owned_manager = ManagerData {
                num_led,
                communication_mode,
                host,
                port,
                serial_port_path: serial_port_path.clone(),
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
                serial_port: None,
                keepalive: true,
            };

            let owned_options = unity_options.clone();

            children.push(thread::spawn(move || {
                match unity::get_events(
                    &mut owned_manager,
                    owned_options.unity_ip,
                    owned_options.unity_ports.clone()[i as usize]
                        .try_into()
                        .unwrap(),
                ) {
                    Ok(_) => {}
                    Err(e) => {
                        panic!("get_events thread crashed with error: {}", e)
                    }
                }
            }))
        }

        for child in children {
            match child.join() {
                Ok(_) => {}
                Err(e) => {
                    error!("Couldn't join child thread {:?}", e)
                }
            };
        }
    }

    // led_manager::set_color(&mut manager, 1, 255, 255, 255);

    flush_data(&mut manager);
}

fn check_and_create_file(file: &PathBuf) -> Result<File, Box<dyn Error>> {
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

fn flush_data(manager: &mut ManagerData) {
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
