use chrono::Local;
use inquire;
use log::{debug, error, info, warn};
use opencv::{
    core::{self, flip, get_default_algorithm_hint, min_max_loc, no_array, Point, Scalar},
    highgui::{self, EVENT_LBUTTONDOWN, EVENT_LBUTTONUP, EVENT_MOUSEMOVE},
    imgproc::{self, COLOR_BGR2GRAY, LINE_8},
    prelude::*,
    videoio::{self, VideoCapture}, Result,
};
use std::{
    cmp::{max, min}, error::Error, fs::File, io::Write, path::Path, process, sync::{Arc, Mutex}, thread, time::Duration
};

use crate::led_manager;
use crate::Config;
use crate::ManagerData;

#[derive(Clone, Debug)]
pub struct ScanData {
    pos: CropPos,
    invert: bool,
    depth: bool,
}

#[derive(Clone, Debug)]
pub struct CropPos {
    x1_start: i32,
    y1_start: i32,
    x1_end: i32,
    y1_end: i32,
    x2_start: Option<i32>,
    y2_start: Option<i32>,
    x2_end: Option<i32>,
    y2_end: Option<i32>,
    cam_1_brightest: Option<f64>,
    cam_2_brightest: Option<f64>,
    cam_1_darkest: Option<f64>,
    cam_2_darkest: Option<f64>
}
type ScanResult = Result<(i32, i32, Option<i32>, Option<i32>), Box<dyn Error>>;
type PosEntry = Vec<(String, (i32, i32), Option<(i32, i32)>)>;

pub fn scan(config: Config, manager_guard: &Arc<Mutex<ManagerData>>) -> Result<()> {
    let manager = &mut manager_guard.lock().unwrap();
    let mut led_pos = vec![("UNCALIBRATED".to_string(), (0, 0), Some((0, 0))); config.num_led as usize];
    info!("Clearing strip");
    for i in 0..=manager.num_led {
        led_manager::set_color(manager_guard, i.try_into().unwrap(), 0, 0, 0);
    }
    let mut pos = match crop(&config) {
        Ok(pos) => pos,
        Err(e) => {
            panic!("There was a problem while trying to crop: {}", e)
        }
    };

    let window = "Please wait...";
    highgui::named_window(window, highgui::WINDOW_AUTOSIZE)?;

    let cam = Arc::new(Mutex::new(videoio::VideoCapture::new(config.camera_index_1, videoio::CAP_ANY)?)); // We need to constantly poll this in the background to get the most recent frame due to OpenCV bug(?)
    let mut cam2: Option<Arc<Mutex<VideoCapture>>> = None;

    let cam_guard = Arc::clone(&cam);
    let cam2_guard;

    thread::spawn(move || {
        loop {
            let mut frame = Mat::default();
            cam_guard.lock().unwrap().read(&mut frame).unwrap();
            thread::sleep(Duration::from_millis(1)); // Give us a chance to grab the lock
        }

    });

    (pos.cam_1_brightest, pos.cam_1_darkest) = match brightest_darkest(&cam, &config, manager_guard, pos.x1_start, pos.y1_start, pos.x1_end, pos.y1_end) {
        Ok((brightest, darkest)) => (Some(brightest), Some(darkest)),
        Err(e) => {
            panic!("There was an issue trying to get the darkest and brightest values: {e}")
        }
    };
    
    if config.multi_camera {
        cam2 = Some(Arc::new(Mutex::new(videoio::VideoCapture::new(config.camera_index_2.unwrap(), videoio::CAP_ANY)?)));

        cam2_guard = Arc::clone(cam2.as_ref().unwrap());
        
        thread::spawn(move || {
            loop {
                let mut frame = Mat::default();
                cam2_guard.lock().unwrap().read(&mut frame).unwrap();
                thread::sleep(Duration::from_millis(1)); // Give us a chance to grab the lock
            }

        });
        let initial_cal_var = brightest_darkest(cam2.as_ref().unwrap(), &config, manager_guard, pos.x1_start, pos.y1_start, pos.x1_end, pos.y1_end);
        (pos.cam_2_brightest, pos.cam_2_darkest) = (
            Some(match initial_cal_var {
                Ok(brightest) => brightest.0,
                Err(e) => {
                    panic!("There was an issue trying to get the darkest and brightest values: {e}");
                }
            }),
            Some(match initial_cal_var {
                Ok(darkest) => darkest.1,
                Err(e) => {
                    panic!("There was an issue trying to get the darkest and brightest values: {e}");
                }
            }),
        );
    }

    let data = Arc::new(Mutex::new(ScanData {
        pos,
        invert: false,
        depth: false,
    }));
    
    info!("Scan XY");
    let (success, failures, success_cam_2, failures_cam_2) = match scan_area(
        manager_guard,
        &config,
        &cam,
        cam2.as_ref(),
        &mut led_pos,
        data.clone(),
    ) {
        Ok((success, failures, success_cam_2, failures_cam_2)) => (success, failures, success_cam_2, failures_cam_2),
        Err(e) => {
            panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
        }
    };

    if !config.multi_camera {
        info!("{success} succesful calibrations, {failures} failed calibrations");
    } else {
        info!("First camera: {success} succesful calibrations, {failures} failed calibrations. \nSecond camera: {} succesful calibrations, {} failed calibrations.", success_cam_2.unwrap(), failures_cam_2.unwrap());
    }

    if failures > 0 {
        // Rescan XY from the back if there are failures
        {
            data.lock().unwrap().invert = true;
        }
        info!("Please rotate the container 180 degrees to recalibrate failures. Press any key to continue.");
        highgui::set_window_title(window, "Please rotate the container 180 degrees to recalibrate failures. Press any key to continue.")?;
        match wait(data.clone(), &cam, window) {
            Ok(_) => {}
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        }
        let (cam_1_success, cam_1_failures, cam_2_success, cam_2_failures) = match scan_area(
            manager_guard,
            &config,
            &cam,
            cam2.as_ref(),
            &mut led_pos,
            data.clone(),
        ) {
            Ok((cam_1_success, cam_1_failures, cam_2_success, cam_2_failures)) => (cam_1_success, cam_1_failures, cam_2_success, cam_2_failures),
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        };
        if !config.multi_camera {
            info!("{cam_1_success} succesful calibrations, {cam_1_failures} failed calibrations");
        } else {
            info!("First camera: {cam_1_success} succesful calibrations, {cam_1_failures} failed calibrations. \nSecond camera: {} succesful calibrations, {} failed calibrations.", cam_2_success.unwrap(), cam_2_failures.unwrap());
        }
        if failures > 0 {
            info!("Entering manual calibration mode!");
            match manual_calibrate(
                manager_guard,
                &config,
                window,
                &cam,
                &mut led_pos,
                &data,
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

    {
        data.lock().unwrap().invert = false;
        data.lock().unwrap().depth = true;
    }

    let (success, failures, success_cam_2, failures_cam_2) = match scan_area(
        manager_guard,
        &config,
        &cam,
        cam2.as_ref(),
        &mut led_pos,
        data.clone(),
    ) {
        Ok((cam_1_success, cam_1_failures, cam_2_success, cam_2_failures)) => (cam_1_success, cam_1_failures, cam_2_success, cam_2_failures),
        Err(e) => {
            panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
        }
    };

    if !config.multi_camera {
        info!("{success} succesful calibrations, {failures} failed calibrations");
    } else {
        info!("First camera: {success} succesful calibrations, {failures} failed calibrations. \nSecond camera: {} succesful calibrations, {} failed calibrations.", success_cam_2.unwrap(), failures_cam_2.unwrap());
    }

    if failures > 0 {
        {
            data.lock().unwrap().invert = true;
        }
        info!("Please rotate the container 180 degrees to recalibrate failures. Press any key to continue.");
        highgui::set_window_title(window, "Please rotate the container 180 degrees to recalibrate failures. Press any key to continue.")?;
        match wait(data.clone(), &cam, window) {
            Ok(_) => {}
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        }
        let (cam_1_success, cam_1_failures, cam_2_success, cam_2_failures) = match scan_area(
            manager_guard,
            &config,
            &cam,
            cam2.as_ref(),
            &mut led_pos,
            data.clone(),
        ) {
            Ok((cam_1_success, cam_1_failures, cam_2_success, cam_2_failures)) => (cam_1_success, cam_1_failures, cam_2_success, cam_2_failures),
            Err(e) => {
                panic!("There was an error trying to scan the XY portion. The data that has been gathered so far has been saved to {}. The error was: {}", failed_calibration(led_pos), e);
            }
        };
        if !config.multi_camera {
            info!("{cam_1_success} succesful calibrations, {cam_1_failures} failed calibrations");
        } else {
            info!("First camera: {cam_1_success} succesful calibrations, {cam_1_failures} failed calibrations. \nSecond camera: {} succesful calibrations, {} failed calibrations.", cam_2_success.unwrap(), cam_2_failures.unwrap());
        }
        if failures > 0 {
            info!("Entering manual calibration mode!");
            match manual_calibrate(
                manager_guard,
                &config,
                window,
                &cam,
                &mut led_pos,
                &data.clone(),
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

pub fn brightest_darkest(cam: &Arc<Mutex<VideoCapture>>, config: &Config, manager: &Arc<Mutex<ManagerData>>, x_start: i32, y_start: i32, x_end: i32, y_end: i32) -> Result<(f64, f64), Box<dyn Error>>  {
    
    let mut cam = cam.lock().unwrap();
    match videoio::VideoCapture::is_opened(&cam)? {
        true => {},
        false => {panic!(
            "Unable to open camera {}! Please select another.",
            config.camera_index_1
        )}
    };

    info!("Collecting brightest and darkest points, please wait...");
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
    let (_, brightest, _) = get_brightest_cam_1_pos(frame.try_clone()?);

    debug!("get darkest_cam_1");
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
    let (_, darkest, _) = get_brightest_cam_1_pos(frame.try_clone()?);

    Ok((brightest, darkest))
    
}

pub fn crop(config: &Config) -> Result<CropPos, Box<dyn Error>> {
    let window = "Calibration";

    let x1_start = Arc::new(Mutex::new(0));
    let y1_start = Arc::new(Mutex::new(0));
    let x1_end = Arc::new(Mutex::new(0));
    let y1_end = Arc::new(Mutex::new(0));

    let x2_start = Arc::new(Mutex::new(0));
    let y2_start = Arc::new(Mutex::new(0));
    let x2_end = Arc::new(Mutex::new(0));
    let y2_end = Arc::new(Mutex::new(0));

    let camera_active = Arc::new(Mutex::new(0)); // 0 for first, 1 for second.

    let x1_start_guard = Arc::clone(&x1_start);
    let y1_start_guard = Arc::clone(&y1_start);
    let x1_end_guard = Arc::clone(&x1_end);
    let y1_end_guard = Arc::clone(&y1_end);

    let x2_start_guard = Arc::clone(&x2_start);
    let y2_start_guard = Arc::clone(&y2_start);
    let x2_end_guard = Arc::clone(&x2_end);
    let y2_end_guard = Arc::clone(&y2_end);

    let camera_active_guard = Arc::clone(&camera_active);
    let mut actively_cropping = false;

    highgui::named_window(window, highgui::WINDOW_AUTOSIZE)?;
    highgui::set_mouse_callback(
        window,
        Some(Box::new(move |event, x, y, _flag| match event {
            #[allow(non_snake_case)]
            // EVENT_LBUTTONDOWN is defined in the OpenCV crate, so I can't change it.
            EVENT_LBUTTONDOWN => {
                actively_cropping = true;
                if *camera_active_guard.lock().unwrap() == 0 {
                    *x1_start_guard.lock().unwrap() = x;
                    *y1_start_guard.lock().unwrap() = y;
                } else if *camera_active_guard.lock().unwrap() == 1 {
                    *x2_start_guard.lock().unwrap() = x;
                    *y2_start_guard.lock().unwrap() = y;
                }
            }
            #[allow(non_snake_case)]
            EVENT_LBUTTONUP => {
                actively_cropping = false;
                if *camera_active_guard.lock().unwrap() == 0 {
                    *x1_end_guard.lock().unwrap() = x;
                    *y1_end_guard.lock().unwrap() = y;
                } else if *camera_active_guard.lock().unwrap() == 1 {
                    *x2_end_guard.lock().unwrap() = x;
                    *y2_end_guard.lock().unwrap() = y;
                }
            }
            #[allow(non_snake_case)]
            EVENT_MOUSEMOVE => {
                if actively_cropping {
                    if *camera_active_guard.lock().unwrap() == 0 {
                        *x1_end_guard.lock().unwrap() = x;
                        *y1_end_guard.lock().unwrap() = y;
                    } else if *camera_active_guard.lock().unwrap() == 1 {
                        *x2_end_guard.lock().unwrap() = x;
                        *y2_end_guard.lock().unwrap() = y;
                    }
                }
            }

            _ => {}
        })),
    )?;

    let cam = videoio::VideoCapture::new(config.camera_index_1, videoio::CAP_ANY)?; // 0 is the default camera
    match videoio::VideoCapture::is_opened(&cam)? {
        true => {},
        false => {panic!(
            "Unable to open camera {}! Please select another.",
            config.camera_index_1
        )}
    };
    let x_start;
    let x_end;
    let y_start;
    let y_end;

    let mut x2_start = None;
    let mut x2_end = None;
    let mut y2_start = None;
    let mut y2_end = None;

    (x_start, x_end, y_start, y_end) = match crop_loop(cam, x1_start.clone(), y1_start.clone(), x1_end.clone(), y1_end.clone(), window, "Please drag the mouse around the container. Press any key to continue".to_string()) {
        Ok((x_start, x_end, y_start, y_end)) => (x_start, x_end, y_start, y_end),
        Err(e) => panic!("Something went wrong during cropping: {e}")
    };

    if let Some(index) = config.camera_index_2 {
        *camera_active.lock().unwrap() = 1;
        let cam = videoio::VideoCapture::new(index, videoio::CAP_ANY)?; // 0 is the default camera
        match videoio::VideoCapture::is_opened(&cam)? {
            true => {},
            false => {panic!(
                "Unable to open camera {}! Please select another.",
                index
            )}
        };
        let loop_out = crop_loop(cam, x1_start, y1_start, x1_end, y1_end, window, "Please drag the mouse around the second container. Press any key to continue".to_string()).unwrap();
        (x2_start, x2_end, y2_start, y2_end) = (
            Some(loop_out.0),
            Some(loop_out.1),
            Some(loop_out.2),
            Some(loop_out.3));
    }

    Ok(CropPos {
        x1_start: x_start,
        y1_start: y_start,
        x1_end: x_end,
        y1_end: y_end,
        x2_start,
        y2_start,
        x2_end,
        y2_end,
        cam_1_brightest: None,
        cam_1_darkest: None,
        cam_2_brightest: None,
        cam_2_darkest: None,
    })

}

pub fn crop_loop(mut cam: VideoCapture, x_start: Arc<Mutex<i32>>, y_start: Arc<Mutex<i32>>, x_end: Arc<Mutex<i32>>, y_end: Arc<Mutex<i32>>, window: &str, msg: String) -> Result<(i32, i32, i32, i32), Box<dyn Error>> {
    info!("{msg}");
    highgui::set_window_title(
        window,
        &msg,
    )
    .unwrap();
    loop {
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
                break Ok((x_start_guard, x_end_guard, y_start_guard, y_end_guard))
            } else {
                error!("Please select a valid are for the crop");
            }
        }
    }
}

pub fn get_brightest_cam_1_pos(mut frame: Mat) -> (f64, f64, Point) {
    imgproc::cvt_color(&frame.clone(), &mut frame, COLOR_BGR2GRAY, 0, get_default_algorithm_hint().unwrap()).unwrap(); // Greyscales frame
    imgproc::gaussian_blur(
        // Blur frame to increase accuracy of min_max_loc
        &frame.clone(),
        &mut frame,
        core::Size::new(41, 41),
        0.0,
        0.0,
        0,
        get_default_algorithm_hint().unwrap()
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
    manager: &Arc<Mutex<ManagerData>>,
    config: &Config,
    cam: &Arc<Mutex<videoio::VideoCapture>>,
    cam2: Option<&Arc<Mutex<videoio::VideoCapture>>>,
    led_pos: &mut PosEntry,
    scan_data: Arc<Mutex<ScanData>>,
) -> ScanResult {
    let scan_data = &mut *scan_data.lock().unwrap();

    let cam_1_window = "Camera 1";
    let cam_2_window = "Camera 2";
    
    let mut success = 0;
    let mut failures = 0;

    let mut cam_2_success = None;
    let mut cam_2_failures = None;

    for i in 0..config.num_led {
        let valid_cycle = if scan_data.depth {
            led_pos[i as usize].0 != "SUCCESS-Z"
        } else {
            !led_pos[i as usize].0.contains("SUCCESS")
        };

        if valid_cycle {
            if config.multi_camera {
                let scan_area_result = scan_area_cycle(manager, cam2, scan_data, led_pos, i, true, cam_2_window).unwrap();
                (cam_2_success, cam_2_failures) = (Some(scan_area_result.0), Some(scan_area_result.1));
            }

            debug!("valid_cycle: {}", i);
            (success, failures) = scan_area_cycle(manager, Some(cam), scan_data, led_pos, i, false, cam_1_window).unwrap();
        }
    }
    Ok((success, failures, cam_2_success, cam_2_failures))
}

pub fn scan_area_cycle(manager: &Arc<Mutex<ManagerData>>, cam: Option<&Arc<Mutex<VideoCapture>>>, scan_data: &mut ScanData, led_pos: &mut PosEntry, i: u32, second_cam: bool, window:&str) -> Result<(i32, i32), Box<dyn Error>> {
    let capture_frames = 1; // Increase me if calibration appears scrambled to ensure the video buffer is empty.

    let mut success = 0;
    let mut failures = 0;

    let x_start;
    let x_end;
    let y_start;
    let y_end;

    if !second_cam {
        x_start = scan_data.pos.x1_start;
        x_end = scan_data.pos.x1_end;
        y_start = scan_data.pos.y1_start;
        y_end = scan_data.pos.y1_end;
    } else {
        x_start = scan_data.pos.x2_start.unwrap();
        x_end = scan_data.pos.x2_end.unwrap();
        y_start = scan_data.pos.y2_start.unwrap();
        y_end = scan_data.pos.y2_end.unwrap();
    }

    led_manager::set_color(manager, i.try_into().unwrap(), 255, 255, 255);
    let mut frame = Mat::default();
    {
        let mut cam = cam.unwrap().lock().unwrap();
        for _ in 0..capture_frames { // This is still needed unfortunately. It may need to be increased if you continue to encounter issues
            cam.read(&mut frame)?;
        }
    }
    let mut frame = Mat::roi(
        &frame,
        opencv::core::Rect {
            x: x_start,
            y: y_start,
            width: x_end - x_start,
            height: y_end - y_start,
        },
    )?
    .try_clone()?;
    if scan_data.invert {
        flip(&frame.clone(), &mut frame, 1).unwrap();
    }
    let (_, max_val, pos) = get_brightest_cam_1_pos(frame.try_clone()?);

    if max_val >= scan_data.pos.cam_1_darkest.unwrap() + ((scan_data.pos.cam_1_brightest.unwrap() - scan_data.pos.cam_1_darkest.unwrap()) * 0.5) {
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
    scan_data_lock: Arc<Mutex<ScanData>>,
    cam: &Arc<Mutex<videoio::VideoCapture>>,
    window: &str,
) -> Result<(), Box<dyn Error>> {
    let scan_data = scan_data_lock.lock().unwrap();
    loop {
        let mut frame = Mat::default();
        {
            let mut cam = cam.lock().unwrap();
            cam.read(&mut frame)?;
        }

        let cropped_image = Mat::roi(
            &frame,
            opencv::core::Rect {
                x: scan_data.pos.x1_start,
                y: scan_data.pos.y1_start,
                width: scan_data.pos.x1_end - scan_data.pos.x1_start,
                height: scan_data.pos.y1_end - scan_data.pos.y1_start,
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
    manager_guard: &Arc<Mutex<ManagerData>>,
    config: &Config,
    window: &str,
    cam: &Arc<Mutex<videoio::VideoCapture>>,
    led_pos: &mut PosEntry,
    scan_data_guard: &Arc<Mutex<ScanData>>,
) -> Result<()> {
    let scan_data = scan_data_guard.lock().unwrap();

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
        led_manager::set_color(manager_guard, led_index as u16, 255, 255, 255);
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
                x: scan_data.pos.x1_start,
                y: scan_data.pos.y1_start,
                width: scan_data.pos.x1_end - scan_data.pos.x1_start,
                height: scan_data.pos.y1_end - scan_data.pos.y1_start,
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
                    led_manager::set_color(manager_guard, led_index.try_into().unwrap(), 0, 0, 0);
                    led_index += 1;
                } else {
                    warn!("At end of LEDs!");
                }
                break;
            } else if key == 101 {
                debug!("got E");
                led_manager::set_color(manager_guard, led_index.try_into().unwrap(), 0, 0, 0);
                if led_index - 1 > 0 {
                    led_index -= 1;
                } else {
                    warn!("At first LED!");
                }
                break;
            } else if key == 102 {
                debug!("got F");
                led_manager::set_color(manager_guard, led_index.try_into().unwrap(), 0, 0, 0);
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