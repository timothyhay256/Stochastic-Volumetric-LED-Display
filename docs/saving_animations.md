# Saving animations (and some info about the webserver)
If you want, you can save animations to either a file or to the ESP directly, and play them back without running Unity, and thus using minimal resources. 
 - **Setting up recording**: The only thing that you need to do is set some variables. Set `data_file` inside `led_manager` to what file animations that can be read by a server should be saved to. Set `esp_data_file` to what file animations that can be saved directly to the ESP should be stored in. Additionally, you will probably want to make sure  `unityControlRecording` is true so that you can start and stop recording from within Unity.

 - **Recording**: To start recording in Unity, go to the GameObject called "Recording Manager" and tick either "Recording" to start recording a regular vled file, or "Recording Byte" to record a bvled file. Now, start doing whatever it is that you want to record, and when finished (or to pause the recording) untick the box. If you recorded a bvled file, you will need to check "Export Byte Data" after recording to actually write the file.

 - **Playing VLED**: To read a vled file and play it back, open the `read_vled.py` file and change the variable `vledFile` to whatever file you want to playback. After that, simply run the script. The animation will play back.

 - **Playing BVLED**: To play a bvled file, you will need to flash the script to read bvled's to your microcontroller. First, find the bvled string(s) you would like to be able to play, and copy the whole thing. It should look something like this: `0x0d, 0x00, .... 0x02 0x0a`  
 Next, find the file `esp_code/read_vled_esp32`, and paste the string into either `animationZero` or `animationOne`. (Note that you can add additional animations quite easily, and can store up to however many your device can store in flash.) And finally, just upload the script! 

 ## Notes
 **vled files**: Can be read by the script `read_vled.py`.  
 The format for this file is super simple. Commands that set a LED are stored as follows:  
 `index|r|g|b`  
 where index is which LED to set, and r, g, and b are color values.  
 Timing is stored like the following:  
 `T:n`  
 where `n` is how long to wait before executing the next instruction.

 **bvled files**: Can be read directly by the ESP.  
 The format for these files is slightly more complex, but still quite simple. Commands that set a LED are stored in 4 bytes, where the first byte is the index, and the subsequent 3 are the RGB values. There is no marker for subsequent instructions, so knowing where in the data you are reading is very important, and a single error will result in the rest of the data being skewed. Timing is stored with 4 marker bytes, being `0xFF, 0x01, 0x02, 0x03` and then a byte representing how long to wait in ms. This does mean any timing longer than 255 ms needs an additional marker byte section.