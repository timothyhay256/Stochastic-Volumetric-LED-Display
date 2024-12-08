from fake_led import main
import threading
fakeEsp = False

if fakeEsp:
    fakeEspThread = threading.Thread(target=main).start() # Needed because get_events will attempt to connect to LEDs on init
from unity_get_event import get_events
from unity_send_pos import send_pos
from unity_animate import send_data
from simpleLog import log

# Set up UDP socket for all Unity communication
UDP_IP = "127.0.0.1"
UDP_PORT = 5001
# UDP_PORT_EVENTS = 5002
# Position data file to read
posFile = "ledPosJarOne.json"

log("Sending position data...")

send_pos(UDP_IP, UDP_PORT, posFile)

log("Starting thread to listen for Unity events...")

eventThread = threading.Thread(target=get_events, args=(UDP_IP,UDP_PORT), kwargs={'communicate_mode':2})
eventThread.start()

# log("Sending headset data to Unity in current thread...")

# try:
#     send_data()
# except ConnectionResetError:
#     print("Unity disconnected. Restart Unity and then restart me. (CTRL-C to kill me)")
#     exit(0)