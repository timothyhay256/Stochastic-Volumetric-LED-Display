# Calibrating the LEDs

## Setup

1. **Configure `led_manager.py`**  
   Most scripts use the settings defined in `led_manager.py` to communicate with the LEDs. Set the required variables at the top of this file.

2. **Update `scan.py`**  
   Open `scan.py` and set the `numLed` variable to the number of LEDs in your strip.

3. **Set Up the Camera**  
   - Ensure the camera is level and ideally at the center height of the container.
   - Set the `camera_index` variable in `scan.py`:
     - `1` is usually for built-in webcams.
     - `2` is usually for other detected cameras.
   - The first LED in the container should have an unobstructed view of the camera. This LED’s brightness is used as a threshold to determine successful calibrations.

## Using `scan.py`

1. **Start Calibration**  
   Run `scan.py`. You should see a window displaying the camera feed. If not, consult `troubleshooting.md`.

2. **Crop the image**  
   The window will prompt: 'Drag the mouse around the container.' Drag the mouse around the container to define its area, then press 'c' to continue.

3. **Initial Calibration**  
   The first LED will illuminate and a circle will be drawn around it. Press 'c' again to calibrate the remaining LEDs.

4. **Handle Calibration Failures**  
   - If some LEDs fail to calibrate, (this is very likely) you will be prompted to rotate the container 180 degrees or move the camera to the other side.
   - Keep the container in the same position relative to its original orientation to avoid errors. Press 'c' to continue.
   - A second calibration run will be performed to calibrate LEDs obstructed by others.

5. **Manual Calibration**  
   - Rotate the container back to its original position.
   - If any LEDs still need calibration, you will enter manual calibration mode. The window title will show `R for next, E for previous`.
   - In this mode, go through each LED, ensure the circle is around the illuminated LED, and click to set a new location if needed.
   - Use 'r' and 'e' to cycle through LEDs, 'f' and 'd' to move to the next or previous failed LED, and 'q' to finish.

6. **Depth Calibration**  
   - After completing the XY calibration, you will be prompted to rotate the jar 90 degrees.
   - Repeat the calibration process for depth. Perform a second run after rotating the container and manually fix any errors.

Once you have completed all of these steps, the positions of the LEDs will be saved to the file specified in the variable `ledPosFile`. This file will be overwritten the next time you run the script, so copy it somewhere safe!

## Things to Note

- **Manual Calibration Data**  
  All changes during manual calibration are saved in `tempPos.json`. This file can be used to recover data if needed.
