# Setting up LEDs
## Supported LED chipsets
Anything that the FastLED library supports will work fine, although WS2811 and friends will be limited to 400 FPS max(When using a ESP32 and serial). This is due to a intentional limitation in FastLED, although it is possible to attempt overclocking the LEDs! See the FastLED wiki for more info.
## Setting up 
If you want to use features such as the builtin web server, stored animations, or the ability to send gyroscope data, you will need to use a ESP32, since these all take advantage of the dual cores. If you just want to push commands to the LEDs from a seperate machine, a ESP8266 does fine (or any PIO compatible microcontroller), although it can only achieve up to 250 FPS on serial.  
Depending on what configuration you are using, you will want to flash a different script to the ESP you are using.  
### Available scripts (located in esp_code/script_name/src/main.cpp)
 - **`read_vled_esp32`** - Read vled binary data that can store animations.  
 - **`receive_esp_serial`** - Receive commands via serial. Either ESP8266 or ESP32.  
 - **`receive_esp8266_udp_parallel`** - Receive commands via UDP. (Much lower FPS than serial, and lots of packet loss.)  
 - **`esp_receive_serial_send_gyro`** - This is what is used in the demo. It receives commands via serial, and sends gyroscope data from a MPU6050 to a target IP address.  
 - **`webserver_jar_esp32`** - This is what is used in the demo video with the jars. It runs a webserver that lets you play various stored animations, lets you change the running animation with a builtin touch pin, has support for reacting to a microphone, and has a mode to accept commands over serial while sending gyroscope data. This can be fairly easily modified to whatever you need/want it to.

Find pin assignments and any variables that you need to set inside the files. And if you are not using WS2811, then make sure to change the chipset type in the FastLED setup inside of `setup()`!
You can flash these using PlatformIO or Arduino.  
Once flashed, you can move on to calibrating the LEDs!  
*Please note that these scripts are still quite messy, and will be updated in the future to be cleaner and more usable.*