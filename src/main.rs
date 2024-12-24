use gumdrop::Options;
use log::{info, warn}; // TODO: Depreceate unity export byte data
use serde::Deserialize;
use serialport::SerialPort;
use std::{
    env,
    fs::File,
    io::{BufWriter, Read},
    net::{Ipv4Addr, UdpSocket},
    path::{Path, PathBuf},
    time::SystemTime,
};

pub mod led_manager;
pub mod speedtest;

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
    failures: u8,
    first_run: bool,       // First ManagerData specific option, above is just Config
    call_time: SystemTime, // Used to persist so we can track time between set_color calls
    data_file_buf: Option<BufWriter<File>>, // On the first run that requires writing to disk, this will be initialized.
    esp_data_file_buf: Option<BufWriter<File>>, // We could either add two new variables to track each ones init state, or we could just init both when either one needs to.
    udp_socket: Option<UdpSocket>, // The second option reduces clutter, and barely reduces performance, so we do that.
    serial_port: Option<Box<dyn SerialPort>>,
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
}

#[derive(Debug, Options)]
struct SpeedtestOptions {}

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

    let print_send_back = config_holder.print_send_back;

    // Validate config and inform user of settings
    if communication_mode == 2 {
        if Path::new(&serial_port_path).exists() {
            info!("Using serial for communication on {}!", serial_port_path);
        } else {
            panic!("Serial port {} does not exist!", serial_port_path);
        }
    } else if communication_mode == 1 {
        info!("Using udp for communication at {} on port {}", host, port);
    }

    if unity_controls_recording || record_data {
        if Path::new(&record_data_file).exists() {
            warn!(
                "{} Will be overwritten once you start recording!",
                record_data_file.display()
            );
        }
        if Path::new(&record_esp_data_file).exists() {
            warn!(
                "{} Will be overwritten once you start recording!",
                record_data_file.display()
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
        serial_port_path,
        baud_rate,
        serial_read_timeout,
        record_data,
        record_data_file,
        record_esp_data,
        unity_controls_recording,
        record_esp_data_file,
        failures: 0,
        print_send_back,
        udp_read_timeout,
        first_run: true,
        call_time: SystemTime::now(),
        data_file_buf: None,
        esp_data_file_buf: None,
        udp_socket: None,
        serial_port: None,
    };

    if let Some(Command::Speedtest(ref _speedtest_options)) = opts.command {
        info!("Performing speedtest...");

        speedtest::speedtest(&mut manager, num_led, 750);
    }

    led_manager::set_color(&mut manager, 1, 255, 255, 255);

    // Remember to flush our buffers at the end.
}
