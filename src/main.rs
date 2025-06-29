use std::{
    env,
    fs::File,
    io::Read,
    path::{Path, PathBuf},
    process,
    sync::{Arc, Mutex},
};

use gumdrop::Options;
use log::{error, info}; // TODO: Depreceate unity export byte data
#[cfg(feature = "gui")]
use svled::gui;
#[cfg(feature = "scan")]
use svled::scan;
use svled::{
    demo, driver_wizard,
    led_manager::{self, set_color},
    read_vled, speedtest,
    unity::{self, start_listeners},
    utils,
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

    #[options(help = "send positions and connect to Unity")]
    Unity(UnityCommandOptions),

    #[options(help = "send positions to Unity")]
    SendPos(SendPosOptions),

    #[options(help = "connect to Unity")]
    ConnectUnity(ConnectUnity),

    #[cfg(feature = "gui")]
    #[options(help = "launch the GUI")]
    Gui(GuiOptions),

    #[options(help = "interactively create a ino/cpp file for your LED driver")]
    DriverWizard(DriverWizardOptions),

    #[options(help = "set a single leds color")]
    SetColor(SetColorOptions),

    #[options(help = "clear the strip")]
    Clear(ClearOptions),

    #[options(help = "run a simple demo")]
    Demo(DemoOptions),

    #[options(help = "convert an led position json into a C++ compatible constant")]
    ConvertLedpos(ConvertLedposOptions),
}

#[derive(Debug, Options)]
struct SpeedtestOptions {}

#[derive(Debug, Options)]
struct ReadvledOptions {
    #[options(help = "vled file to read", required)]
    vled_file: PathBuf,
}

#[derive(Debug, Options)]
struct SetColorOptions {
    #[options(help = "index of LED", required)]
    n: u16,
    #[options(help = "R value to set", required)]
    r: u8,
    #[options(help = "G value to set", required)]
    g: u8,
    #[options(help = "B value to set", required)]
    b: u8,
}

#[cfg(feature = "scan")]
#[derive(Debug, Options)]
struct CalibrateOptions {}

#[derive(Debug, Options)]
struct UnityCommandOptions {}

#[derive(Debug, Options)]
struct SendPosOptions {}

#[derive(Debug, Options)]
struct ConnectUnity {}

#[cfg(feature = "gui")]
#[derive(Debug, Options)]
struct GuiOptions {}

#[derive(Debug, Options)]
struct DriverWizardOptions {}

#[derive(Debug, Options)]
struct DemoOptions {
    #[options(help = "demo to run (rainbow, rainbow-loop)", required)]
    active_demo: String,
}

#[derive(Debug, Options)]
struct ClearOptions {}

#[derive(Debug, Options)]
struct ConvertLedposOptions {
    #[options(help = "input JSON file", required)]
    input: String,

    #[options(help = "output file")]
    output: Option<String>,
}

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

    if let Some(Command::ConvertLedpos(ref convert_ledpos_options)) = opts.command {
        let path = Path::new(&convert_ledpos_options.input);

        info!(
            "Converting {} into an C++ compatible data format",
            path.display()
        );

        if path.exists() {
            let mut pos_file = match File::open(path) {
                Ok(file) => file,
                Err(e) => {
                    panic!("Could not read {:?}: {}", path.display(), e)
                }
            };

            let mut file_contents = String::new();
            match pos_file.read_to_string(&mut file_contents) {
                Ok(_) => {}
                Err(e) => {
                    panic!("Could not read position file {}: {}", path.display(), e)
                }
            };

            let json: JsonEntry = match serde_json::from_str(&file_contents) {
                Ok(json) => json,
                Err(e) => {
                    panic!(
                        "{} contains invalid or incomplete calibration data: {}",
                        path.display(),
                        e
                    )
                }
            };

            let mut output_string = String::new();

            let len = json.len();

            output_string.push_str(&format!(
                "static constexpr std::array<std::array<int16_t, 2>, {len}> coords = {{{{"
            ));

            for (index, entry) in json.iter().enumerate() {
                output_string.push_str(&format!(
                    "{{{}, {}}}, {{{}, {}}}",
                    entry.1 .0, entry.1 .1, entry.2 .0, entry.2 .1
                ));

                if index == len - 1 {
                    output_string.push_str("}};")
                } else {
                    output_string.push_str(", ");
                }
            }

            info!("{output_string}");
        } else {
            error!("{} not found", path.display());
        }
        return;
    }

    let config_load_result = utils::load_validate_conf(config_path);

    let (manager, unity_options, config_holder) = (
        Arc::new(Mutex::new(config_load_result.0)),
        config_load_result.1,
        config_load_result.2,
    );

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
                panic!("There was an issue connecting to Unity: {e}");
            }
        };

        info!("Spawning listening threads");

        start_listeners(&config_holder, &manager);
    } else if let Some(Command::SendPos(ref _sendpos_options)) = opts.command {
        info!("Sending positions to Unity");

        match unity::send_pos(unity_options.clone()) {
            Ok(_) => {}
            Err(e) => {
                panic!("There was an issue connecting to Unity: {e}");
            }
        };
    } else if let Some(Command::ConnectUnity(ref _connectunity_options)) = opts.command {
        info!("Spawning listening threads");

        start_listeners(&config_holder, &manager);
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
    } else if let Some(Command::Demo(ref demo_options)) = opts.command {
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

        match demo_options.active_demo.to_lowercase().as_str() {
            "rainbow-loop" => loop {
                demo::rainbow(&manager, &json, 80.0, 50.0, false, demo::Axis::X, true);
                demo::rainbow(&manager, &json, 50.0, 50.0, false, demo::Axis::Y, true);
                demo::rainbow(&manager, &json, 80.0, 50.0, false, demo::Axis::Z, true);
            },
            "rainbow" => {
                demo::rainbow(&manager, &json, 80.0, 50.0, false, demo::Axis::X, true);
                demo::rainbow(&manager, &json, 50.0, 50.0, false, demo::Axis::Y, true);
                demo::rainbow(&manager, &json, 80.0, 50.0, false, demo::Axis::Z, true);
            }
            option => {
                error!("Invalid option {option}");
            }
        }
    } else if let Some(Command::Clear(ref _clear_options)) = opts.command {
        for n in 0..config_holder.num_led {
            led_manager::set_color(&manager, n as u16, 0, 0, 0);
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
