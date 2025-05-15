#[cfg(feature = "gui")]
pub mod gui;

#[cfg(feature = "scan")]
pub mod scan;

pub mod demo;
pub mod driver_wizard;
pub mod led_manager;
pub mod read_vled;
pub mod speedtest;
pub mod unity;
pub mod utils;

// #[cfg(feature = "gui")]
// pub use gui::main;

#[cfg(feature = "scan")]
pub use scan::scan;

pub use demo::rainbow;
pub use driver_wizard::wizard;
pub use led_manager::set_color;
pub use read_vled::read_vled;
pub use speedtest::speedtest;
pub use unity::{get_events, send_pos, signal_restart};
pub use utils::*;
