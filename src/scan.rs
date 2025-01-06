use chrono::Local;
use inquire;
use log::{debug, error, info, warn};
use opencv::{
    core::{self, flip, min_max_loc, no_array, Point, Scalar},
    highgui,
    highgui::{EVENT_LBUTTONDOWN, EVENT_LBUTTONUP, EVENT_MOUSEMOVE},
    imgproc::{self, COLOR_BGR2GRAY, LINE_8},
    prelude::*,
    videoio, Result,
};
use std::{
    cmp::{max, min}, error::Error, fs::File, io::Write, path::Path, process, sync::{Arc, Mutex}, thread, time::Duration
}; // This will be used for more things in the future, so it's not bloat

use crate::led_manager;
use crate::Config;
use crate::ManagerData;

#[derive(Clone, Debug)]
pub struct ScanData {
    x_start: i32,
    y_start: i32,
    x_end: i32,
    y_end: i32,
    darkest: f64,
    brightest: f64,
    invert: bool,
    depth: bool,
}
type PosEntry = Vec<(String, (i32, i32), Option<(i32, i32)>)>;

pub fn scan(config: Config, manager: &mut ManagerData) -> Result<()> {
    let mut led_pos: PosEntry =
        vec![("UNCALIBRATED".to_string(), (0, 0), Some((0, 0))); config.num_led as usize];
    info!("Clearing strip");
    for i in 0..=manager.num_led {
        led_manager::set_color(manager, i.try_into().unwrap(), 0, 0, 0);
    }
    let (x_start, y_start, x_end, y_end) = match crop(&config) {
        Ok((x_start, y_start, x_end, y_end)) => (x_start, y_start, x_end, y_end),
        Err(e) => {
            panic!("There was a problem while trying to crop: {}", e)
        }
    };

    let window = "Please wait...";
    highgui::named_window(window, highgui::WINDOW_AUTOSIZE)?;

    let cam = Arc::new(Mutex::new(videoio::VideoCapture::new(config.camera_index, videoio::CAP_ANY)?)); // We need to constantly poll this in the background to get the most recent frame due to OpenCV bug(?)

    let cam_guard = Arc::clone(&cam);
    
    thread::spawn(move || {
        loop {
            let mut frame = Mat::default();
            cam_guard.lock().unwrap().read(&mut frame).unwrap();
            thread::sleep(Duration::from_millis(1)); // Give us a chance to grab the lock
        }

    });
    let brightest;
    let darkest;
    {
        let mut cam = cam.lock().unwrap();
        let opened = videoio::VideoCapture::is_opened(&cam)?;
        if !opened {
            panic!(
                "Unable to open camera {}! Please select another.",
                config.camera_index
            );
        }

        info!("Collecting brightest and darkest points, please wait...");
        debug!("Get brightest");
        led_manager::set_color(manager, 5, 255, 255, 255);

        let mut frame = Mat::default();
        cam.read(&mut frame)?;
        let frame = Mat::roi(
            &frame,
            opencv::core::Rect {
                x: x_start,
                y: y_start,
                width: x_end - x_start,
                height: y_end - y_start,
            },
        )?;
        (_, brightest, _) = get_brightest_pos(frame.try_clone()?);

        debug!("get darkest");
        led_manager::set_color(manager, 5, 0, 0, 0);

        let mut frame = Mat::default();
        cam.read(&mut frame)?;
        let frame = Mat::roi(
            &frame,
            opencv::core::Rect {
                x: x_start,
                y: y_start,
                width: x_end - x_start,
                height: y_end - y_start,
            },
        )?;
        (_, darkest, _) = get_brightest_pos(frame.try_clone()?);
    }

    let mut data = ScanData {
        x_start,
        y_start,
        x_end,
        y_end,
        darkest,
        brightest,
        invert: false,
        depth: false,
    };
    info!("Scan XY");
    let (success, failures) = match scan_area(
        manager,
        &config,
        window,
        &cam,
        &mut led_pos,
        data.clone(),
    ) {
        Ok((success, failures)) => (success, failures),
        Err(e) => {
            panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
        }
    };

    info!("{success} succesful calibrations, {failures} failed calibrations");

    if failures > 0 {
        // Rescan XY from the back if there are failures
        data.invert = true;
        info!("Please rotate the container 180 degrees to recalibrate failures. Press any key to continue.");
        highgui::set_window_title(window, "Please rotate the container 180 degrees to recalibrate failures. Press any key to continue.")?;
        match wait(data.clone(), &cam, window) {
            Ok(_) => {}
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        }
        let (success, failures) = match scan_area(
            manager,
            &config,
            window,
            &cam,
            &mut led_pos,
            data.clone(),
        ) {
            Ok((success, failures)) => (success, failures),
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        };
        info!("{success} succesful calibrations, {failures} failed calibrations");
        if failures > 0 {
            info!("Entering manual calibration mode!");
            match manual_calibrate(
                manager,
                &config,
                window,
                &cam,
                &mut led_pos,
                data.clone(),
            ) {
                Ok(_) => {}
                Err(e) => {
                    panic!("Something went wrong during manual calibration: {}", e);
                }
            }
        }
        info!("Please rotate the container 270 degrees to calibrate Z. Press any key to continue."); // The LEDS will be 180 degrees away from the original position, and they need to be rotated 270 degrees in this case to go to the appropriate Z calibration position.
        highgui::set_window_title(
            window,
            "Please rotate the container 270 degrees to calibrate Z. Press any key to continue.",
        )?;
    }

    if failures == 0 {
        info!("Please rotate the container 90 degrees to calibrate Z. Press any key to continue.");
        highgui::set_window_title(
            window,
            "Please rotate the container 90 degrees to calibrate Z. Press any key to continue.",
        )?;
    }
    match wait(data.clone(), &cam, window) {
        Ok(_) => {}
        Err(e) => {
            panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
        }
    }
    info!("Scan Z");

    data.invert = false;
    data.depth = true;
    let (success, failures) = match scan_area(
        manager,
        &config,
        window,
        &cam,
        &mut led_pos,
        data.clone(),
    ) {
        Ok((success, failures)) => (success, failures),
        Err(e) => {
            panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
        }
    };

    info!("{success} succesful calibrations, {failures} failed calibrations");

    if failures > 0 {
        data.invert = true;
        info!("Please rotate the container 180 degrees to recalibrate failures. Press any key to continue.");
        highgui::set_window_title(window, "Please rotate the container 180 degrees to recalibrate failures. Press any key to continue.")?;
        match wait(data.clone(), &cam, window) {
            Ok(_) => {}
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        }
        let (success, failures) = match scan_area(
            manager,
            &config,
            window,
            &cam,
            &mut led_pos,
            data.clone(),
        ) {
            Ok((success, failures)) => (success, failures),
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        };
        info!("{success} succesful calibrations, {failures} failed calibrations");
        if failures > 0 {
            info!("Entering manual calibration mode!");
            match manual_calibrate(
                manager,
                &config,
                window,
                &cam,
                &mut led_pos,
                data.clone(),
            ) {
                Ok(_) => {}
                Err(e) => {
                    panic!("Something went wrong during manual calibration: {}", e);
                }
            }
        }
    }
    highgui::destroy_all_windows().unwrap();
    post_process(&mut led_pos, manager.num_led);
    loop {
        let date = Local::now();
        let name = inquire::Text::new(&format!(
            "File name:({}-ledpos.json)",
            date.format("%Y-%m-%d-%H:%M:%S")
        ))
        .prompt();

        match name {
            Ok(mut name) => {
                if name.is_empty(){
                    name = format!("{}-ledpos.json", date.format("%Y-%m-%d-%H:%M:%S"));
                }
                let json = serde_json::to_string_pretty(&led_pos).expect("Unable to serialize metadata!");
                let mut file = match File::create(Path::new(&name)) {
                    Ok(file) => file,
                    Err(e) => {
                        error!(
                            "Unable to write temp-pos to {name}"
                        );
                        println!("Something went wrong trying to save the LED positions. What has been collected has been written to {}. Error: {}", failed_calibration(led_pos.clone()), e);
                        process::exit(1);
                    }
                };
                
                match file.write_all(json.as_bytes()) {
                    Ok(_) => {},
                    Err(e) => {
                        error!(
                            "Unable to write temp-pos to {name}"
                        );
                        println!("Something went wrong trying to save the LED positions. What has been collected has been written to {}. Error: {}", failed_calibration(led_pos.clone()), e);
                    }
                }
                break;
            }
            Err(_) => println!("Something went wrong trying to save the LED positions. What has been collected has been written to {}.", failed_calibration(led_pos.clone())),
        };
    }
    Ok(())
}

pub fn crop(config: &Config) -> Result<(i32, i32, i32, i32), Box<dyn Error>> {
    let window = "Calibration";
    let x_start = Arc::new(Mutex::new(0));
    let y_start = Arc::new(Mutex::new(0));
    let x_end = Arc::new(Mutex::new(0));
    let y_end = Arc::new(Mutex::new(0));

    highgui::named_window(window, highgui::WINDOW_AUTOSIZE)?;

    let x_start_guard = Arc::clone(&x_start);
    let y_start_guard = Arc::clone(&y_start);
    let x_end_guard = Arc::clone(&x_end);
    let y_end_guard = Arc::clone(&y_end);
    let mut actively_cropping = false;

    highgui::set_mouse_callback(
        window,
        Some(Box::new(move |event, x, y, _flag| match event {
            #[allow(non_snake_case)]
            // EVENT_LBUTTONDOWN is defined in the OpenCV crate, so I can't change it.
            EVENT_LBUTTONDOWN => {
                actively_cropping = true;
                *x_start_guard.lock().unwrap() = x;
                *y_start_guard.lock().unwrap() = y;
            }
            #[allow(non_snake_case)]
            EVENT_LBUTTONUP => {
                actively_cropping = false;
                *x_end_guard.lock().unwrap() = x;
                *y_end_guard.lock().unwrap() = y;
            }
            #[allow(non_snake_case)]
            EVENT_MOUSEMOVE => {
                if actively_cropping {
                    *x_end_guard.lock().unwrap() = x;
                    *y_end_guard.lock().unwrap() = y;
                }
            }

            _ => {}
        })),
    )?;

    let mut cam = videoio::VideoCapture::new(config.camera_index, videoio::CAP_ANY)?; // 0 is the default camera
    let opened = videoio::VideoCapture::is_opened(&cam)?;
    if !opened {
        panic!(
            "Unable to open camera {}! Please select another.",
            config.camera_index
        );
    }
    info!("Please drag the mouse around the container. Press any key to continue");
    highgui::set_window_title(
        window,
        "Please drag the mouse around the container. Press any key to continue",
    )
    .unwrap();
    Ok(loop {
        let mut frame = Mat::default();
        cam.read(&mut frame)?;

        let x_end_guard = *x_end.lock().unwrap();
        let y_end_guard = *y_end.lock().unwrap();
        let x_start_guard = *x_start.lock().unwrap();
        let y_start_guard = *y_start.lock().unwrap();

        let rect = core::Rect::new(
            x_start_guard,
            y_start_guard,
            x_end_guard - x_start_guard,
            y_end_guard - y_start_guard,
        );

        imgproc::rectangle(
            &mut frame,
            rect,
            core::Scalar::new(255.0, 0.0, 0.0, 0.0),
            3,
            8,
            0,
        )
        .expect("tf bro erroring on a rectangle?");

        if frame.size()?.width > 0 {
            highgui::imshow(window, &frame)?;
        }
        let key = highgui::wait_key(10)?;
        if key > 0 && key != 255 {
            if x_start_guard != 0 && x_end_guard != 0 {
                highgui::destroy_all_windows().unwrap();
                break (x_start_guard, y_start_guard, x_end_guard, y_end_guard);
            } else {
                error!("Please select a valid are for the crop");
            }
        }
    })
}

pub fn get_brightest_pos(mut frame: Mat) -> (f64, f64, Point) {
    imgproc::cvt_color(&frame.clone(), &mut frame, COLOR_BGR2GRAY, 0).unwrap(); // Greyscales frame
    imgproc::gaussian_blur(
        // Blur frame to increase accuracy of min_max_loc
        &frame.clone(),
        &mut frame,
        core::Size::new(41, 41),
        0.0,
        0.0,
        0,
    )
    .unwrap();

    let mut min_val = 0.0;
    let mut max_val = 0.0;
    let mut max_loc = Point::new(0, 0);

    min_max_loc(
        &frame,
        Some(&mut min_val),
        Some(&mut max_val),
        None,
        Some(&mut max_loc),
        &no_array(),
    )
    .unwrap();

    (min_val, max_val, max_loc)
}

pub fn scan_area(
    manager: &mut ManagerData,
    config: &Config,
    window: &str,
    cam: &Arc<Mutex<videoio::VideoCapture>>,
    led_pos: &mut PosEntry,
    scan_data: ScanData,
) -> Result<(i32, i32), Box<dyn Error>> {

    let capture_frames = 3; // Increase me if calibration appears scrambled to ensure the video buffer is empty.
    
    let mut success = 0;
    let mut failures = 0;
    for i in 0..config.num_led {
        let valid_cycle = if scan_data.depth {
            led_pos[i as usize].0 != "SUCCESS-Z"
        } else {
            !led_pos[i as usize].0.contains("SUCCESS")
        };

        if valid_cycle {
            debug!("valid_cycle: {}", i);
            led_manager::set_color(manager, i.try_into().unwrap(), 255, 255, 255);
            let mut frame = Mat::default();
            {
                let mut cam = cam.lock().unwrap();
                for _ in 0..capture_frames { // This is still needed unfortunately. It may need to be increased if you continue to encounter issues
                    cam.read(&mut frame)?;
                }
            }
            let mut frame = Mat::roi(
                &frame,
                opencv::core::Rect {
                    x: scan_data.x_start,
                    y: scan_data.y_start,
                    width: scan_data.x_end - scan_data.x_start,
                    height: scan_data.y_end - scan_data.y_start,
                },
            )?
            .try_clone()?;
            if scan_data.invert {
                flip(&frame.clone(), &mut frame, 1).unwrap();
            }
            let (_, max_val, pos) = get_brightest_pos(frame.try_clone()?);

            if max_val >= scan_data.darkest + ((scan_data.brightest - scan_data.darkest) * 0.5) {
                debug!("Succesful xy calibration: {:?} on index: {}", pos, i);
                success += 1;
                imgproc::circle(
                    &mut frame,
                    pos,
                    20,
                    Scalar::new(0.0, 255.0, 0.0, 255.0),
                    2,
                    LINE_8,
                    0,
                )?;
                if scan_data.depth {
                    led_pos[i as usize] = (
                        "SUCCESS-Z".to_string(),
                        led_pos[i as usize].1,
                        Some((pos.x, pos.y)),
                    );
                } else {
                    led_pos[i as usize] = (
                        "SUCCESS-XY".to_string(),
                        (pos.x, pos.y),
                        led_pos[i as usize].2,
                    );
                }
            } else {
                debug!("Failed xy calibration: {:?} on index: {}", pos, i);
                failures += 1;
                imgproc::circle(
                    &mut frame,
                    pos,
                    20,
                    Scalar::new(0.0, 0.0, 255.0, 255.0),
                    2,
                    LINE_8,
                    0,
                )?;
                if scan_data.depth {
                    led_pos[i as usize] = (
                        "RECALIBRATE-Z".to_string(),
                        led_pos[i as usize].1,
                        Some((pos.x, pos.y)),
                    );
                } else {
                    led_pos[i as usize] = (
                        "RECALIBRATE-XY".to_string(),
                        (pos.x, pos.y),
                        led_pos[i as usize].2,
                    );
                }
            }
            highgui::set_window_title(window, &("LED index: ".to_owned() + &i.to_string()))?;
            highgui::imshow(window, &frame)?;
            highgui::wait_key(1)?;
            led_manager::set_color(manager, i.try_into().unwrap(), 0, 0, 0);
        }
    }
    Ok((success, failures))
}

pub fn failed_calibration(led_pos: PosEntry) -> String {
    let json = serde_json::to_string_pretty(&led_pos).expect("Unable to serialize metadata!");

    let date = Local::now();
    let path = format!("temp-pos-{}", date.format("%Y-%m-%d-%H:%M:%S"));
    let mut file = File::create(Path::new(&path)).unwrap_or_else(|_| {
        panic!(
            "Unable to write temp-pos to {path}, temp-pos: {:?}",
            led_pos
        )
    });
    file.write_all(json.as_bytes())
        .unwrap_or_else(|_| panic!("Unable to write temp-pos file at {path}"));
    path
}

pub fn wait(
    scan_data: ScanData,
    cam: &Arc<Mutex<videoio::VideoCapture>>,
    window: &str,
) -> Result<(), Box<dyn Error>> {
    loop {
        let mut frame = Mat::default();
        {
            let mut cam = cam.lock().unwrap();
            cam.read(&mut frame)?;
        }

        let cropped_image = Mat::roi(
            &frame,
            opencv::core::Rect {
                x: scan_data.x_start,
                y: scan_data.y_start,
                width: scan_data.x_end - scan_data.x_start,
                height: scan_data.y_end - scan_data.y_start,
            },
        )
        .unwrap();

        if frame.size()?.width > 0 {
            highgui::imshow(window, &cropped_image)?;
        }

        let key = highgui::wait_key(10)?;
        if key > 0 && key != 255 {
            break;
        }
    }
    Ok(())
}

pub fn manual_calibrate(
    manager: &mut ManagerData,
    config: &Config,
    window: &str,
    cam: &Arc<Mutex<videoio::VideoCapture>>,
    led_pos: &mut PosEntry,
    scan_data: ScanData,
) -> Result<()> {
    debug!("scan_data: {:?}", scan_data);
    info!("You are entering manual calibration mode. \n
    This is to manually calibrate all LEDs that failed to properly calibrate, and to make sure all LEDs did calibrate properly. The controls are:\n
    R: Move to the next LED
    E: Move to the previous LED
    F: Move to the next uncalibrated LED
    Left Click: Select the illuminated LED.
    Q: Exit calibration and move on.
    
    Any LEDs that are thought to need recalibration will be circled red, and the LEDs that are thought to be accurate will be blue.");

    let x_click = Arc::new(Mutex::new(0));
    let y_click = Arc::new(Mutex::new(0));
    let callback_called = Arc::new(Mutex::new(false));

    let x_click_guard = Arc::clone(&x_click);
    let y_click_guard = Arc::clone(&y_click);
    let callback_called_guard = Arc::clone(&callback_called);

    highgui::set_mouse_callback(
        window,
        Some(Box::new(move |event, x, y, _flag| {
            if event == EVENT_LBUTTONDOWN {
                *callback_called_guard.lock().unwrap() = true;
                *x_click_guard.lock().unwrap() = x;
                *y_click_guard.lock().unwrap() = y;
            }
        })),
    )?;

    let mut led_index: usize = 0;
    'video: loop {
        debug!("led_index: {}", led_index);
        highgui::set_window_title(
            window,
            &format!(
                "R for next, E for previous, Q to finish. On LED {}",
                led_index,
            ),
        )
        .unwrap();
        led_manager::set_color(manager, led_index as u8, 255, 255, 255);
        let mut frame = Mat::default();
        {
            let mut cam = cam.lock().unwrap();
            for _ in 0..3 {
                cam.read(&mut frame)?;
            }
        }

        let mut frame = Mat::roi(
            &frame,
            opencv::core::Rect {
                x: scan_data.x_start,
                y: scan_data.y_start,
                width: scan_data.x_end - scan_data.x_start,
                height: scan_data.y_end - scan_data.y_start,
            },
        )
        .unwrap()
        .try_clone()?;
        if scan_data.invert {
            debug!("inverting display");
            flip(&frame.clone(), &mut frame, 1).unwrap();
        }

        let mut color = Scalar::new(0.0, 0.0, 255.0, 255.0);
        if !led_pos[led_index].0.contains("RECALIBRATE") {
            color = Scalar::new(0.0, 255.0, 0.0, 255.0);
        }
        let pos;
        if scan_data.depth {
            if !*callback_called.lock().unwrap() {
                debug!("pos is from depth, callback uncalled.");
                pos = match led_pos[led_index].2 {
                    Some(pos) => Point::new(pos.0, pos.1),
                    None => {
                        error!("led_pos does not contain any depth data, setting to 0, 0!");
                        Point::new(0, 0)
                    }
                };
            } else {
                debug!("pos is from callback");
                led_pos[led_index].0 = "MANUAL-Z".to_string();
                led_pos[led_index].2 = Some((*x_click.lock().unwrap(), *y_click.lock().unwrap()));
                pos = Point::new(*x_click.lock().unwrap(), *y_click.lock().unwrap());
                color = Scalar::new(0.0, 255.0, 0.0, 255.0);
                *callback_called.lock().unwrap() = false;
            }
        } else if !*callback_called.lock().unwrap() {
            debug!("pos not from depth, from led_pos[led_index].1 which is {:?}", led_pos[led_index].1);
            pos = Point::new(led_pos[led_index].1 .0, led_pos[led_index].1 .1)
        } else {
            debug!("pos not from depth, from callback");
            led_pos[led_index].0 = "MANUAL-XY".to_string();
            led_pos[led_index].1 = (*x_click.lock().unwrap(), *y_click.lock().unwrap());
            pos = Point::new(*x_click.lock().unwrap(), *y_click.lock().unwrap());
            color = Scalar::new(0.0, 255.0, 0.0, 255.0);
            *callback_called.lock().unwrap() = false;
        }
        debug!("setting cricle at {:?}", pos);
        imgproc::circle(&mut frame, pos, 20, color, 2, LINE_8, 0)?;

        if frame.size()?.width > 0 {
            highgui::imshow(window, &frame)?;
        }

        loop {
            if *callback_called.lock().unwrap() {
                debug!("Breaking key loop on detected callback!");
                break;
            }
            let key = highgui::wait_key(10)?;
            if key == 114 {
                debug!("got R");
                if led_index + 1 < config.num_led.try_into().unwrap() {
                    led_manager::set_color(manager, led_index.try_into().unwrap(), 0, 0, 0);
                    led_index += 1;
                } else {
                    warn!("At end of LEDs!");
                }
                break;
            } else if key == 101 {
                debug!("got E");
                led_manager::set_color(manager, led_index.try_into().unwrap(), 0, 0, 0);
                if led_index - 1 > 0 {
                    led_index -= 1;
                } else {
                    warn!("At first LED!");
                }
                break;
            } else if key == 102 {
                debug!("got F");
                led_manager::set_color(manager, led_index.try_into().unwrap(), 0, 0, 0);
                let led_begin = led_index; // Needed because of clippy::mut_range_bound
                for _ in led_begin..config.num_led.try_into().unwrap() {
                    led_index += 1;
                    if led_pos[led_index].0.contains("RECALIBRATE") {
                        break;
                    }
                }
                break;
            } else if key == 113 {
                break 'video;
            }
        }
    }
    Ok(())
}

pub fn post_process(led_pos: &mut PosEntry, led_count: u32) {
    let mut y_max = i32::MIN;
    let mut z_max = i32::MIN;

    let mut x_min = i32::MAX;
    let mut y_min = i32::MAX;
    let mut z_min = i32::MAX;
    
    for i in 0..led_count { // Get max and min values in led_pos
        x_min = min(led_pos[i as usize].1.0, x_min);

        y_max = max(led_pos[i as usize].1.1, y_max);
        y_min = min(led_pos[i as usize].1.1, y_min);

        z_min = min(led_pos[i as usize].2.unwrap().0, z_min);
        z_max = max(led_pos[i as usize].2.unwrap().0, z_max);
    }

    for i in 0..led_count { // Normalize values
        led_pos[i as usize].1.0 -= x_min;
        led_pos[i as usize].1.1 -= y_min;
        led_pos[i as usize].2.unwrap().0 = led_pos[i as usize].2.unwrap().0 - z_min;
    }

    for i in 0..led_count {
        let y_mid = y_max / 2;
        let current_y = led_pos[i as usize].1.1;

        let z_mid = y_max / 2;
        let current_z = led_pos[i as usize].2.unwrap().0;

        led_pos[i as usize].1.1 = match current_y {
            y if y > y_mid => y_mid - (y - y_mid),
            y if y < y_mid => y_mid + (y_mid - y),
            _ => led_pos[i as usize].1.1, // when current_y == y_mid, no change
        };
        

        if current_z > z_mid {
            led_pos[i as usize].2.unwrap().0 = z_mid - (current_z - z_mid);
        } else if current_y < y_mid {
            led_pos[i as usize].2.unwrap().0 = z_mid + (z_mid - current_z);
        }
    }
}