Format for readable animation data:
index|r|g|b
Eindex
T:3.4030420394320

Format for byte animation data:
byte(index)byte(r)byte(g)byte(b)

byte(1)byte(1)byte(1)byte(1)byte(time in ms) # Time in ms, and have multiple time instructions for timing longer than 255 ms. This should be fine, since there should be few gaps more than 255 ms. Note that it is unlikely that the first index will be set to (1, 1, 1) so this is what is used to indicate a timing instruction. TODO: Make the program recognize when it is writing this instruction and change it to (1, 2, 1) or something.



I can manage to reach around 250 fps on the ESP8266 using Serial.
I can manage to reach the max speed for WS2811 at 400 fps on the ESP32 using Serial sometimes, but it is spotty for some reason.
ESP32 not tested actually wired yet.