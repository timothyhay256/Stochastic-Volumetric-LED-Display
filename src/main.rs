use gumdrop::Options;
use log::{debug, error, info}; // TODO: Depreceate unity export byte data
use std::{
    env,
    fs::File,
    io::Read,
    path::{Path, PathBuf},
    process,
    sync::{Arc, Mutex},
    thread::{self},
    time::SystemTime,
};
#[cfg(feature = "gui")]
use svled::gui;

#[cfg(feature = "scan")]
use svled::scan;

use svled::{
    demo, driver_wizard, led_manager::set_color, read_vled, speedtest, unity, utils, IOHandles,
    LedState, ManagerData, ManagerState, RuntimeConfig, VisionData,
};

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

    #[cfg(feature = "scan")]
    #[options(help = "calibrate a svled container")]
    Calibrate(CalibrateOptions),

    #[options(help = "connect to Unity")]
    Unity(UnityCommandOptions),
    #[cfg(feature = "gui")]
    #[options(help = "launch the GUI")]
    Gui(GuiOptions),

    #[options(help = "interactively create a ino/cpp file for your LED driver")]
    DriverWizard(DriverWizardOptions),

    #[options(help = "set a single led's color")]
    SetColor(SetColorOptions),

    #[options(help = "run a simple demo")]
    Demo(DemoOptions),
}

#[derive(Debug, Options)]
struct SpeedtestOptions {}

#[derive(Debug, Options)]
struct ReadvledOptions {
    #[options(help = "vled file to read")]
    vled_file: PathBuf,
}

#[derive(Debug, Options)]
struct SetColorOptions {
    #[options(help = "index of LED")]
    n: u16,
    #[options(help = "R value to set")]
    r: u8,
    #[options(help = "G value to set")]
    g: u8,
    #[options(help = "B value to set")]
    b: u8,
}

#[cfg(feature = "scan")]
#[derive(Debug, Options)]
struct CalibrateOptions {}

#[derive(Debug, Options)]
struct UnityCommandOptions {}

#[cfg(feature = "gui")]
#[derive(Debug, Options)]
struct GuiOptions {}

#[derive(Debug, Options)]
struct DriverWizardOptions {}

#[derive(Debug, Options)]
struct DemoOptions {}

type JsonEntry = Vec<(String, (f32, f32), (f32, f32))>;

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

    let config_load_result = utils::load_validate_conf(config_path);

    let (manager, unity_options, config_holder) = (
        Arc::new(Mutex::new(config_load_result.0)),
        config_load_result.1,
        config_load_result.2,
    );

    // ctrlc::set_handler(move || {
    //     handler_manager.lock().unwrap().keepalive = false;
    //     process::exit(0);
    // })
    // .expect("Error setting Ctrl-C handler");

    if let Some(Command::Speedtest(ref _speedtest_options)) = opts.command {
        info!("Performing speedtest...");

        speedtest::speedtest(&manager, config_holder.num_led, 10000);
    } else if let Some(Command::ReadVled(ref readvled_options)) = opts.command {
        if !readvled_options.vled_file.is_file() {
            error!("You must pass a valid vled file with --vled-file!");
            process::exit(0);
        } else {
            info!("Playing back {}!", readvled_options.vled_file.display());

            {
                manager.lock().unwrap().config.record_data = false;
                manager.lock().unwrap().config.record_esp_data = false;
            }
            match read_vled::read_vled(&manager, readvled_options.vled_file.clone()) {
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
            debug!("Spawning listening thread.");

            let owned_manager;

            {
                let manager = manager.lock().unwrap();
                owned_manager = Arc::new(Mutex::new(ManagerData {
                    config: RuntimeConfig {
                        num_led: config_holder.num_led,
                        num_strips: config_holder.num_strips,
                        communication_mode: config_holder.communication_mode,
                        host: config_holder.host,
                        port: config_holder.port,
                        serial_port_paths: manager.config.serial_port_paths.clone(),
                        baud_rate: config_holder.baud_rate,
                        serial_read_timeout: manager.config.serial_read_timeout,
                        record_data: manager.config.record_data,
                        record_data_file: manager.config.record_data_file.clone(),
                        record_esp_data: manager.config.record_esp_data,
                        unity_controls_recording: manager.config.unity_controls_recording,
                        record_esp_data_file: manager.config.record_esp_data_file.clone(),
                        print_send_back: config_holder.advanced.print_send_back,
                        udp_read_timeout: config_holder.advanced.udp_read_timeout,
                        con_fail_limit: config_holder.advanced.con_fail_limit,
                        no_controller: config_holder.advanced.no_controller,
                        scan_mode: config_holder.scan_mode,
                        filter_color: config_holder.filter_color,
                        filter_range: config_holder.filter_range,
                        hsv_red_override: config_holder.advanced.hsv_red_override.clone(),
                        hsv_green_override: config_holder.advanced.hsv_green_override.clone(),
                        hsv_blue_override: config_holder.advanced.hsv_blue_override.clone(),
                        no_video: config_holder.advanced.no_video,
                        skip_confirmation: config_holder.advanced.skip_confirmation,
                        use_queue: config_holder.advanced.use_queue,
                        queue_size: config_holder.advanced.queue_size,
                        led_config: None, // This will be constructed as needed by led_manager
                    },
                    state: ManagerState {
                        first_run: true,
                        call_time: SystemTime::now(),
                        keepalive: true,
                        led_state: LedState {
                            failures: 0,
                            queue_lengths: Vec::new(),
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
                }));
            }

            let owned_options = unity_options.clone();
            let owned_config = config_holder.clone();
            children.push(
                thread::Builder::new()
                    .name("get_events".to_string())
                    .spawn(move || {
                        debug!("inside thread");
                        match unity::get_events(
                            owned_manager,
                            &owned_options.clone(),
                            &owned_config,
                            &owned_options.unity_ports.clone()[i as usize],
                            &None,
                        ) {
                            Ok(_) => {
                                debug!("thread exited??")
                            }
                            Err(e) => {
                                panic!("get_events thread crashed with error: {}", e)
                            }
                        }
                    })
                    .unwrap(),
            )
        }

        for child in children {
            match child.join() {
                Ok(_) => {}
                Err(e) => {
                    error!("Couldn't join child thread {:?}", e)
                }
            };
        }
    } else if let Some(Command::DriverWizard(ref _driver_wizard_options)) = opts.command {
        info!("Starting driver configuration wizard!");
        driver_wizard::wizard();
    } else if let Some(Command::SetColor(ref set_color_options)) = opts.command {
        info!(
            "Setting LED {} to RGB: {}, {}, {}",
            set_color_options.n, set_color_options.r, set_color_options.g, set_color_options.b
        );

        set_color(
            &manager,
            set_color_options.n,
            set_color_options.r,
            set_color_options.g,
            set_color_options.b,
        );
    } else if let Some(Command::Demo(ref _demo_options)) = opts.command {
        info!("Running demo!");

        let mut pos_file = match File::open(unity_options.unity_position_files[0].clone()) {
            Ok(file) => file,
            Err(e) => {
                panic!(
                    "Could not read {:?}: {}",
                    unity_options.unity_position_files[0], e
                )
            }
        };

        let mut file_contents = String::new();
        match pos_file.read_to_string(&mut file_contents) {
            Ok(_) => {}
            Err(e) => {
                panic!(
                    "Could not read position file {}: {}",
                    unity_options.unity_position_files[0].display(),
                    e
                )
            }
        };

        let json: JsonEntry = match serde_json::from_str(&file_contents) {
            Ok(json) => json,
            Err(e) => {
                panic!(
                    "{} contains invalid or incomplete calibration data: {}",
                    unity_options.unity_position_files[0].display(),
                    e
                )
            }
        };

        loop {
            demo::rainbow(&manager, &json, 80.0, 50.0, false, demo::Axis::X, true);
            demo::rainbow(&manager, &json, 50.0, 50.0, false, demo::Axis::Y, true);
            demo::rainbow(&manager, &json, 80.0, 50.0, false, demo::Axis::Z, true);
        }
    }

    // #[cfg(feature = "gui")]
    // if let Some(Command::Gui(ref _gui_options)) = opts.command {
    //     gui::main(config_holder.clone()).unwrap();
    // }

    #[cfg(feature = "scan")]
    if let Some(Command::Calibrate(ref _calibrate_options)) = opts.command {
        info!("Performing calibrating");
        scan::scan(config_holder.clone(), &manager, false, None).expect("failure");
    }

    // led_manager::set_color(&mut manager, 1, 255, 255, 255);

    utils::flush_data(manager);
}
