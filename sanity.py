import json
import cv2
from receive_esp8266.esp8266_udp import set_color
import time

numLed = 50 #num of leds
vid = cv2.VideoCapture(2)
ledPos=[[0]*2]*50
first = True
winname = "Calibrating..."
cv2.namedWindow(winname)        # Create a named window
cv2.moveWindow(winname, 2040,30)  # Move it to (40,30)

print("Calibrating...")
for i in range(numLed):
#while True:
    global imageF
    set_color(i, 255, 255, 255)
    ret, image = vid.read()
    set_color(i, 0, 0, 0)
    #cv2.imshow('frame', image
    orig = image.copy()
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    gray = cv2.GaussianBlur(gray, (41, 41), 0)
    (minVal, maxVal, minLoc, maxLoc) = cv2.minMaxLoc(gray)
    image = orig.copy()
    cv2.circle(image, maxLoc, 20, (255, 0, 0), 2)
    ledPos[i] = maxLoc
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break
    cv2.imshow(winname, image)
print(ledPos)
vid.release()
for i in range(50):
    ret, image2 = vid.read()
    cv2.imshow("final", image2)
cv2.destroyWindow(winname)
for i in range(numLed):
    set_color(i, 0, 0, 0)
conf = input("Write collected data?[y/N] ")
if conf.lower() == "y":
    with open('ledPos.json', 'w') as f:
        json.dump(ledPos, f)
print("Done.")

#print(ledPos)
