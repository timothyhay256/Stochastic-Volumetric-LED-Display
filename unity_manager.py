import multiprocessing # TODO: Support UDP and support default modes specified in led_manager
from unity_get_event import get_events
from unity_send_pos import send_pos
from unity_animate import send_data
from simpleLog import log
import os
os.system('./start.sh') # Set jars to accept commands (Remove me)

# Set variables below
total_containers = 1 # How many containers are you using?

UDP_IP = "127.0.0.1" # Unity IP (don't change unless Unity is running somewhere else)
UDP_PORTS = [5001, 5002, 5025, 5004, 5005] # Ports to send or receive data from (These need to be set in Unity as well!)

SERIAL_PORTS = ["/dev/ttyUSB0", "/dev/ttyUSB1", "/dev/ttyUSB2"] # Set to either serial ports or IP addresses to send LED data to.
SERIAL_BAUDRATE = 921600

POSITION_FILES = ["ledPosJarThree.json", "ledPosJarTwo.json", "ledPosJarThree.json"] # Position data files to get position data from to send to Unity
scale = .08 # Multiply positions by this value so that the "LEDS" are not too far apart. Change to be higher if they aren't close enough, or lower if they are too close.
# Set variables above

processes = []
log("Sending positions...")

for i in range(total_containers):
    print(i)
    send_pos(UDP_IP, UDP_PORTS[i], POSITION_FILES[i], scale=scale)
    log("Starting proccess to receive events...")
    p = multiprocessing.Process(target=get_events, args=(UDP_IP, UDP_PORTS[i], 2, SERIAL_PORTS[i], SERIAL_BAUDRATE))
    processes.append(p)
    p.start()
    print("fin")

# log("Starting processes to receive events from Unity...")

# for i in range(total_containers): 
    # p = multiprocessing.Process(target=get_events, args=(UDP_IP, UDP_PORTS[i], 2, SERIAL_PORTS[i], SERIAL_BAUDRATE))
    # processes.append(p)
    # p.start()

for p in processes:
    p.join()
