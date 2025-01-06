fn main() {
    if cfg!(feature = "gui") {
        #[cfg(feature = "gui")]
        use slint_build::CompilerConfiguration;

        #[cfg(feature = "gui")]
        let config = CompilerConfiguration::new().with_style("material".into());
        #[cfg(feature = "gui")]
        slint_build::compile_with_config("ui/app-window.slint", config)
            .expect("Slint build failed");
    } else {
        println!("GUI feature is not enabled. Skipping Slint build.");
    }
}
