# Simple 3d layer test.
import json
from receive_esp8266.esp8266_udp import set_color, save_esp_dat
from simpleLog import log
from time import sleep

'''
Single index in list contains:
['STATUS', (x, y), (x, y)]
Where [0] is the status of the calibration
[1] is the coords for XY calibration
[2] is the coords for Z calibration
'''
esp_data_file = 'udpOut.bvled'

f = open('ledPos.json')
ledPos = json.load(f)

y = 200 # Desired level to draw an layer
fuzz = 25 # Range of coordinates to consider within layer
step = 10 # Step by this many per layer

highest = 0
lowest = 0

on = []
numOn = 0

for i in range(len(ledPos)-1): # Account for crop data in last element
    if ledPos[i][1][1] > highest:
        highest = ledPos[i][1][1]
    if ledPos[i][1][1] < lowest:
        lowest = ledPos[i][1][1]

print(highest)
print(lowest)


# log("Setting single layer")
# for i in range(len(ledPos)):
#     # log(ledPos[i][1][0]) # Should get X coord for XY calibration
#     # log(ledPos[i][2][0]) # Should get X coord for Z calibration

#     if y-fuzz <= ledPos[i][1][1] <= y+fuzz: # Should set color for a single layer only
#         set_color(i, 255, 255, 255)

# sleep(5)
# for i in range(len(ledPos)):
#         set_color(i, 0, 0, 0)

def flow(flip=False, clear=True):
    global on, numOn # for j in range(highest-step, lowest, -1*step): # Should make a layer of lights flow up the jar
    if flip:
        x = highest-step
        y = lowest
        z = -1*step
    else:
        x = lowest
        y = highest
        z = step
    for j in range(x, y, z): # Should make a layer of lights flow up the jar
        # print(j)
        # for i in range(len(ledPos)):
        #     set_color(i, 0, 0, 0)
        # sleep(.5) # Remove me, just for testing
        for i in range(len(ledPos)):
            if j-fuzz <= ledPos[i][1][1] <= j+fuzz: # Should set color for a single layer only
                # print(i)
                # r = int(255 * (j - lowest) / (highest - lowest)) 
                r = 255
                g = int(255 * (highest - j) / (highest - lowest)) 
                b = int(255 * (highest - j) / (highest - lowest))
                on.append(i)
                set_color(i, r, g, b)
                numOn += 1
                # print("G is "+str(g))
        # sleep(.5)
        if clear:
            for i in range(len(on)):
                # print(on)
                # print(i)
                if flip:
                    if (j-step)-fuzz <= ledPos[on[i]][1][1] <= (j-step)+fuzz and j+step <= highest: # If the next layer wants to have the LED, leave it on.
                        pass
                    else:
                        set_color(on[i], 0, 0, 0)
                else:
                    if (j+step)-fuzz <= ledPos[on[i]][1][1] <= (j+step)+fuzz and j+step <= highest: # If the next layer wants to have the LED, leave it on.
                        pass
                    else:
                        set_color(on[i], 0, 0, 0)
        on = []
        numOn = 0

# while True:
    # flow()

# flow()
# flow(True)

# flow()
# flow(True, False)

# save_esp_dat(esp_data_file)

for i in range(len(ledPos)):
    set_color(i, 0, 4, 25)
# flow()

"""
g = int(255 * (j - lowest) / (highest - lowest)) 
r = int(255 * (highest - j) / (highest - lowest))
b = 255
Light purple to cyan 

Light pink to blue
g = int(255 * (j - lowest) / (highest - lowest)) 
r = int(255 * (highest - j) / (highest - lowest)) 
b = int(255 * (j - lowest) / (highest - lowest)) 

white to red
r = 255
g = int(255 * (highest - j) / (highest - lowest)) 
b = int(255 * (highest - j) / (highest - lowest))
"""