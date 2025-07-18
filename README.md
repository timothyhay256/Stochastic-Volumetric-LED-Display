# SVLED
## Stochastic Volumetric LEDs (3D Mapped LEDs [pretty looking lights])
*Hello those from Open Sauce!*

This project allows you to scan a container full of individually addressible RGB leds, and create a representation of those LEDs in 3d space in Unity/Houdini, where you can apply whatever effects in 3d that you desire.  
It also has the ability to save animations either to a file, or directly to the LED controller (ESP32 and friends). 

Additionally, it supports multiple controllers on the same strip, meaning that it can drive very large amounts of LEDs very fast (~20,000 LEDs per second with 40 controllers and 2,000 LEDs).  

https://github.com/user-attachments/assets/2d306b9b-878b-488d-bfa9-6dc4f7c6ec3e

## Usage

If you want to use this project, you will need a couple of things:
 - LEDs - anything that is supported by FastLED will work fine.
 - Any microcontroller supported by FastLED and PlatformIO. You may need a dual core controller depending on the number of LEDs and features that you want.  
 
Once you have the hardware, go to the [Wiki.](https://github.com/timothyhay256/Stochastic-volumetric-LED-display/wiki/Setting-up-LEDs)  

## Commercial Licensing

This project is licensed under the GNU General Public License version 3 or later (GPLv3 or later) for open-source use.

**Proprietary/Commercial License Available**  
If you are a company or organization wishing to use this software in a proprietary, closed-source product or with different licensing terms, commercial licenses are available.

Please contact me at: svled@stay-silly.org  
Custom licensing is available at a minor cost, depending on your use case.
