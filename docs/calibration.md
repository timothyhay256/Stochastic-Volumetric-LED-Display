# Calibrating the LEDs

## Setup

1. **Configure `svled.toml`**  
   Change the settings inside of `svled.toml` to match your configuration. There are explanations for each value inside the file.  

3. **Set Up the Camera**  
   - Make sure the camera is level, and viewing the container of LEDs ideally at the midpoint of it's height.
   - The fifth LED should have an unobstructed view of the camera, as it is used for determining the brightness threshold to qualify a succesful calibration.
## Scanning the LEDs

1. **Start Calibration**  
   To start calibration, run `svled calibrate` or `cargo run -- calibrate`. A window should pop up.

2. **Crop the image**  
   The window will prompt: 'Drag the mouse around the container.' Drag the mouse around the container to crop the calibration area, and press any key to continue.

3. **Initial Calibration**  
   The LEDs will now be calibrated. In the window, you will see the LED illuminated and circled with either a green or red circle. If the circle is green, the program thinks it succesfully calibrated the LEDs position. If it is red, it thinks it hasn't succesfully calibrated the LED.

4. **Calibration Failures**  
   If some LEDs fail to calibrate, you will be prompted to rotate the container 180 degrees or move the camera to the other side.
   Keep the container in the same position relative to its original orientation to avoid errors. Press any key to continue.
   A second calibration run will be performed to calibrate LEDs obstructed by others LEDs.

5. **Manual Calibration**  
   Once the second run completes, if there are still calibration failures, then you will be dropped into manual calibration mode, where you will need to select each LED with the mouse to identify and calibrate its position.  
   Control keybinds will be printed to the console, but here they are for reference:  
    - **R:** Move to the next LED  
    - **E:** Move to the previous LED  
    - **F:** Move to the next uncalibrated LED  
    - **Left Click:** Select the illuminated LED.  
    - **Q:** Exit calibration and move on.  
    

6. **Depth Calibration**  
   After completing the XY calibration, you will be prompted to rotate the jar 90 degrees, or 270 degrees, depending on what orientation it currently is in. 
   Repeat steps 3-5.

Once you have finished calibration, you will be prompted to save the position data to a file.  

## Things to Note

- **Improper calibration**  
  If you observe that the resulting data appeaers to be completely scrambled, 1: ensure you are running in release mode (if running with Cargo), and if that doesn't work, increase `capture_frames` inside `scan.rs` to something higher, like 5. This is due to OpenCV not always providing the most recent frame, and depending on the hardware setup, the buffer may be larger than `3`. This does have the side effect of slowing down calibration by quite a bit unfortunately.
