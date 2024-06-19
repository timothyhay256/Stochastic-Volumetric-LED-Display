from fake_led import main
import threading
fakeEsp = True

if fakeEsp:
    fakeEspThread = threading.Thread(target=main).start()
from unity_get_event import get_events
from unity_send_pos import send_pos
from unity_animate import send_data
from simpleLog import log

log("Sending position...")

try:
    send_pos()
except ConnectionRefusedError:
    print("\n\nUnity refused to connect. Make sure Unity is running, then reset me. (CTRL-C to kill me)\n\n")

log("Starting thread to listen for Unity events...")

eventThread = threading.Thread(target=get_events)
eventThread.start()

log("Sending headset data to Unity in current thread...")

try:
    send_data()
except ConnectionResetError:
    print("Unity disconnected. Restart Unity and then restart me. (CTRL-C to kill me)")
    exit(0)