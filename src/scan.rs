use chrono::Local;
use log::{debug, error, info};
use opencv::core::{self, flip, no_array, Scalar};
use opencv::core::{min_max_loc, Point};
use opencv::imgproc::COLOR_BGR2GRAY;
use opencv::imgproc::{self, LINE_8};
use opencv::{
    highgui, highgui::EVENT_LBUTTONDOWN, highgui::EVENT_LBUTTONUP, highgui::EVENT_MOUSEMOVE,
    prelude::*, videoio, Result,
};
use std::error::Error;
use std::fs::File;
use std::io::Write;
use std::path::Path;
use std::sync::{Arc, Mutex};

use crate::led_manager;
use crate::Config;
use crate::ManagerData;

#[derive(Clone)]
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
            highgui::destroy_all_windows().unwrap();
            if x_start_guard != 0 && x_end_guard != 0 {
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
    cam: &mut videoio::VideoCapture,
    led_pos: &mut PosEntry,
    scan_data: ScanData,
) -> Result<(i32, i32), Box<dyn Error>> {
    let mut success = 0;
    let mut failures = 0;
    for i in 0..config.num_led {
        if led_pos[i as usize].0 != "SUCCESS-XY" && led_pos[i as usize].0 != "SUCCESSS-Z" {
            led_manager::set_color(manager, i.try_into().unwrap(), 255, 255, 255);
            let mut frame = Mat::default();
            cam.read(&mut frame)?;
            if scan_data.invert {
                flip(&frame.clone(), &mut frame, 1).unwrap();
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
            let (_, max_val, pos) = get_brightest_pos(frame.try_clone()?);

            if max_val >= scan_data.darkest + ((scan_data.brightest - scan_data.darkest) * 0.5) {
                debug!("Succesful xy calibration: {:?}", pos);
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
                debug!("Failed xy calibration: {:?}", pos);
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
            highgui::wait_key(10)?;
            highgui::imshow(window, &frame)?;
            led_manager::set_color(manager, i.try_into().unwrap(), 0, 0, 0);
        }
    }
    Ok((success, failures))
}

pub fn scan(config: Config, manager: &mut ManagerData) -> Result<()> {
    let mut led_pos: PosEntry =
        vec![("UNCALIBRATED".to_string(), (0, 0), Some((0, 0))); config.num_led as usize];
    info!("Clearing strip");
    // for i in 1..=manager.num_led {
    //     led_manager::set_color(manager, i.try_into().unwrap(), 0, 0, 0);
    // }
    let (x_start, y_start, x_end, y_end) = match crop(&config) {
        Ok((x_start, y_start, x_end, y_end)) => (x_start, y_start, x_end, y_end),
        Err(e) => {
            panic!("There was a problem while trying to crop: {}", e)
        }
    };

    let window = "Please wait...";
    highgui::named_window(window, highgui::WINDOW_AUTOSIZE)?;
    let mut cam = videoio::VideoCapture::new(config.camera_index, videoio::CAP_ANY)?; // 0 is the default camera
    let opened = videoio::VideoCapture::is_opened(&cam)?;
    if !opened {
        panic!(
            "Unable to open camera {}! Please select another.",
            config.camera_index
        );
    }

    info!("Collecting brightest and darkest points, please wait...");
    debug!("Get brightest");
    for i in 1..=manager.num_led {
        led_manager::set_color(manager, i.try_into().unwrap(), 255, 255, 255);
    }

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
    let (_, brightest, _) = get_brightest_pos(frame.try_clone()?);

    debug!("get darkest");
    for i in 1..=manager.num_led {
        led_manager::set_color(manager, i.try_into().unwrap(), 0, 0, 0);
    }

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
    let (_, darkest, _) = get_brightest_pos(frame.try_clone()?);

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
        &mut cam,
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
        match wait(data.clone(), &mut cam, window) {
            Ok(_) => {}
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        }
        let (_, _) = match scan_area(
            manager,
            &config,
            window,
            &mut cam,
            &mut led_pos,
            data.clone(),
        ) {
            Ok((success, failures)) => (success, failures),
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        };
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
    match wait(data.clone(), &mut cam, window) {
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
        &mut cam,
        &mut led_pos,
        data.clone(),
    ) {
        Ok((success, failures)) => (success, failures),
        Err(e) => {
            panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
        }
    };

    info!("{success} succesful calibrations, {failures} failed calibrations");

    if failures < 0 {
        data.invert = true;
        info!("Please rotate the container 180 degrees to recalibrate failures. Press any key to continue.");
        highgui::set_window_title(window, "Please rotate the container 180 degrees to recalibrate failures. Press any key to continue.")?;
        match wait(data.clone(), &mut cam, window) {
            Ok(_) => {}
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        }
        let (_, _) = match scan_area(
            manager,
            &config,
            window,
            &mut cam,
            &mut led_pos,
            data.clone(),
        ) {
            Ok((success, failures)) => (success, failures),
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        };
    }

    Ok(())
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
    cam: &mut videoio::VideoCapture,
    window: &str,
) -> Result<(), Box<dyn Error>> {
    loop {
        let mut frame = Mat::default();
        cam.read(&mut frame)?;

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
