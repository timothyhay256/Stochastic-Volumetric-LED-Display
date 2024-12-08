# Using the LEDs in Unity
- **Setup unity_manager**: To setup the script that will talk to Unity, open up the file called `unity_manager.py`, and set the variables inside appropriately. There is an explanation within the file for which variables to set and to what. Make sure that the container you are using is on and able to receive commands, to test this, run this command in the project directory: `python -c 'from led_manager import set_color;set_color(1, 0, 0, 0)'`. If it completes without any errors(such as reached packet timeout), then you are correctly talking to the LEDs. As long as the settings inside `led_manager` for communication are the same as in `unity_manager`, you shouldn't have any issues sending commands to the LEDs.
- **Setup Unity**: To use the LEDs with Unity, open up Unity Hub, and add a project from disk. Go to the folder `unity/Volumetric-Led` and select it. Now open the project from Unity Hub. Once it has opened, open the scene called `SimpleSetup` inside the Vol_LED/Scenes folder. 
 - **Using Unity**: To start, click play on Unity. Once it starts, it will take you to the game tab. Go back to the scene tab. Now, run the `unity_manager.py` script. You should see many spheres spawn in the scene. Each sphere is a LED in it's position that was mapped earlier. Now try taking a object and dragging it into the LEDs, and you should see a rough representation of what you are dragging through the LEDs in the container that you are using. It is now working! 
## Adding additional containers
If you want to run more than one individual container at once, there are a couple of things that you need to do. 
 - **Configure Unity**: Open Unity and find the GameObject named "Container 1", and duplicate it and move it away from the original GameObject. In the new GameObject, go to the child "LEDHold">"LED" and change the connection port in the inspector to currentPort+1. Additionally, go to the child "Spawner" and change the connection port to the same port you set previously. Do this for each additional container that you wish to create, just ensure that there are no ports that two containers share.
 - **Configure the script**: In the script, simply add another item to each variable that is applicable. For example, this would be valid for using just 2 containers:  
 `total_containers = 2`  
`UDP_IP = "127.0.0.1"`  
`UDP_PORTS = [5001, 5002]`  
`SERIAL_PORTS = ["/dev/ttyUSB0", "/dev/ttyUSB1"]`     
`SERIAL_BAUDRATE = 921600`  
`POSITION_FILES = ["ledPosContainerOne.json", "ledPosContainerTwo.json"]`  

And now simply run the project and script again, and there should be multiple containers.
## Notes
You can set a Y modifier that the LEDs will spawn in either by moving the Container GameObject up and down, or by setting the variable yMod in `unity_send_pos.py`