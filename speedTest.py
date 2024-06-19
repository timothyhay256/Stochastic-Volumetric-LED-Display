from receive_esp8266.esp8266_udp import set_color
import time
from simpleLog import log
from random import randint

numLed = 200

# log("All white!")
# for i in range(numLed):
#     set_color(i, 255, 255, 255)
# time.sleep(1)

# log("All random!")
# for i in range(numLed):
#     set_color(i, randint(0, 255), randint(0, 255), randint(0 ,255))
# time.sleep(1)

# log("Testing 15 sequential loops.") # 750 writes
# start = time.time()
# for i in range (15):
#     for i in range(numLed):
#         set_color(i, randint(0, 255), randint(0, 255), randint(0 ,255))
# end = time.time()
# log(str(end-start)+" seconds. ")
# log(str((end-start)/750)+" seconds per LED.")
# log(str(750/(end-start))+" LEDs per second.\n\n")

log("Testing 750 random writes.") 
start = time.time()
for i in range (750):
    set_color(randint(0, numLed), randint(0, 255), randint(0, 255), randint(0, 255))
end = time.time()
log(str(end-start)+" seconds. ")
log(str((end-start)/750)+" seconds per LED.")
log(str(750/(end-start))+" LEDs per second.")