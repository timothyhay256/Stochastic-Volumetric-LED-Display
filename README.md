# SVLED
## Stochastic Volumetric LEDs (3D Mapped LEDs [pretty looking lights])
*Proper documentation and more demo videos are coming soon.*

This project allows you to scan a container full of individually addressible RGB leds, and create a representation of those LEDs in 3d space in Unity, where you can apply whatever effects in 3d that you desire.  
It also has the ability to save animations either to a file, or directly to the LED controller (ESP32). 

Additionally, it supports multiple controllers on the same strip, meaning that it can drive very large amounts of LEDs very fast (~9,500 LEDs per second with 20 controllers and 1,000 LEDs).  

You can see a demo video setting up the LEDs here: `TODO: make video`  

You can see a short demo video here: `TODO: make video`  

https://github.com/user-attachments/assets/2d306b9b-878b-488d-bfa9-6dc4f7c6ec3e

## Usage
### The wiki is currently woefully out of date. It should be updated relatively soon.

If you want to use this project, you will need a couple of things:
 - LEDs - anything that is supported by FastLED will work fine.
 - A ESP32 or ESP8266 depending on the number of LEDs and features that you want  
 
Once you have the hardware, go to the [Wiki.](https://github.com/timothyhay256/Stochastic-volumetric-LED-display/wiki/Setting-up-LEDs)

