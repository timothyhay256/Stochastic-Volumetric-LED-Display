import json # Most of this code was written in like a week during school, so don't expect good code! It works though.
import cv2, queue, threading, time
from led_manager import set_color # This function blocks until the color has been properly set.
from simpleLog import log
import time

numLed = 100 # Set number of LEDs here
camera_index = 2 # Which camera should OpenCV use?
ledPosFile = "ledPos.json" # File to write position data to

ledPos=[[0]*3]*(numLed+1)
brightest = 0 # maxVal of first LED
darkest = 0 # maxVal of last LED
first = True
winname = "Calibrating..." # Needed to manipulate OpenCV window.
block = False
selected = False
nextFrame = False
tempFile = "tempPos.json"

success = 0 # Track successfull calibrations
fail = 0
man_x = 0
man_y = 0
man_enable = False
deadzone = (0, 0) # Likely to have no LEDs located here

refPoint = []
x_start, y_start, x_end, y_end = 0, 0, 0, 0
cropping = False
temp = open(tempFile, 'w')

# bufferless VideoCapture from https://stackoverflow.com/questions/43665208/how-to-get-the-latest-frame-from-capture-device-camera-in-opencv#54755738
class VideoCapture:

  def __init__(self, name):
    self.cap = cv2.VideoCapture(name)
    self.q = queue.Queue()
    t = threading.Thread(target=self._reader)
    t.daemon = True
    t.start()

  # read frames as soon as they are available, keeping only most recent one
  def _reader(self):
    # self.cap.set(cv2.CAP_PROP_EXPOSURE, -100)
    while True:
      ret, frame = self.cap.read()
      if not ret:
        break
      if not self.q.empty():
        try:
          self.q.get_nowait()   # discard previous (unprocessed) frame
        except queue.Empty:
          pass
      self.q.put(frame)

  def read(self):
    return self.q.get()

def mouse_crop(event, x, y, flags, param):
    global x_start, y_start, x_end, y_end, cropping, refPoint, ledPos, block
    if not block:
      cropping = False
      # if the left mouse button was DOWN, start RECORDING
      # (x, y) coordinates and indicate that cropping is being
      if event == cv2.EVENT_LBUTTONDOWN:
          # log("EVENT_LBUTTONDOWN")
          x_start, y_start, x_end, y_end = x, y, x, y
          cropping = True
      # Mouse is Moving
      elif event == cv2.EVENT_MOUSEMOVE:
          # log("EVENT_MOUSEMOVE")
          if cropping == True:
              x_end, y_end = x, y
      # if the left mouse button was released
      elif event == cv2.EVENT_LBUTTONUP:
          # log("EVENT_LBUTTONUP")
          # record the ending (x, y) coordinates
          x_end, y_end = x, y
          cropping = False
          refPoint = [(x_start, y_start), (x_end, y_end)]
          log("refPoint is now "+str(refPoint))

def mouse_manual(event, x, y, flags, param):
    global man_x, man_y, man_enable, selected, nextFrame
    # print("fds")
    if man_enable:
        # print("man_enable")
        if event == cv2.EVENT_LBUTTONDOWN:
            selected = True
            nextFrame = True
            man_x, man_y = x, y
            print(str(man_x))
            print(str(man_y))

def manual_calibrate(ledPos, depth=False):  # mode=1 for all, 2 for only failed
  # TODO: Support depth calculation (write to third item in list)
    global selected, winname, man_enable, nextFrame
    man_enable = True
    nextFrame = True
    nextFail = False
    prevFail = False
    cv2.setMouseCallback(winname, mouse_manual) # For manually selecting uncalibrated LEDs
    index = 0
    log("Press R to go to the next LED. Press E to go to the previous. Please click on the LED's location. The best guess will be circled.")
    cv2.setWindowTitle(winname, "R for next, E for previous")
    success = 0 
    set_color(0, 255, 255, 255)
    while True:
        if nextFrame:
            nextFrame = False
            if nextFail:
                nextFail = False
                while ledPos[index][0] != 'FAIL' and ledPos[index][0] != 'RECALIBRATE' and ledPos[index][0] != 'RECALIBRATE-Z':
                    print(index)
                    print(ledPos[index][0])
                    set_color(index, 0, 0, 0)
                    if 0 <= index <= numLed: 
                        index += 1
            if prevFail:
                prevFail = False
                while ledPos[index][0] != 'FAIL' and ledPos[index][0] != 'RECALIBRATE' and ledPos[index][0] != 'RECALIBRATE-Z':
                    set_color(index, 0, 0, 0)
                    index -= 1
            set_color(index, 255, 255, 255)
            time.sleep(.15)
            image = vid.read()[refPoint[0][1]:refPoint[1][1], refPoint[0][0]:refPoint[1][0]]
            temp.write("\n"+str(ledPos)) # Write to temp file in case of crash or failure
            if ledPos[index][0] == 'SUCCESS':
                print("drawing success")
                if depth:
                    log("Drawing depth for success")
                    cv2.circle(image, ledPos[index][2], 20, (0, 255, 0), 2)
                else:
                    cv2.circle(image, ledPos[index][1], 20, (0, 255, 0), 2) 
            elif ledPos[index][0] == 'FAIL' or ledPos[index][0] == 'RECALIBRATE' or ledPos[index][0] == 'RECALIBRATE-Z':
                print("drawing failure")
                if depth:
                    log("Drawing failure for depth")
                    cv2.circle(image, ledPos[index][2], 20, (0, 0, 255), 2)
                else:
                    cv2.circle(image, ledPos[index][1], 20, (0, 0, 255), 2)
            else:
                if depth:
                    log("Drawing success for depth")
                    cv2.circle(image, ledPos[index][2], 20, (255, 0, 0), 2)
                else:
                    cv2.circle(image, ledPos[index][1], 20, (255, 0, 0), 2)
            if selected:
                print("drawing selected")
                cv2.circle(image, (man_x, man_y), 20, (255, 0, 0), 2)
                if depth:
                    log("Drawing manual for depth")
                    ledPos[index][2] = (man_x, man_y)
                    ledPos[index][0] = 'MANUAL-Z' # TODO: Check if bug fixed
                else:
                    ledPos[index] = ['MANUAL', (man_x, man_y), 'NONE']
            cv2.setWindowTitle(winname, "R for next, E for previous. i:"+str(index))
            cv2.imshow(winname, image)
        key = cv2.waitKey(10)
        if key == ord('r'):
            set_color(index, 0, 0, 0)
            if 0 <= index <= numLed:
                index += 1
            print("Adding index, is "+str(index))
            selected = False
            nextFrame = True
        elif key == ord('e'):
            set_color(index, 0, 0, 0)
            if 0 <= index <= numLed:
                index -= 1
            print("Subtracting index, is "+str(index))
            selected = False
            nextFrame = True
        elif key == ord('q'):
            selected = False
            break
        elif key == ord('f'):
            nextFrame = True
            nextFail = True
        elif key == ord('d'):
            nextFrame = True
            prevFail = True

def getBrightest(ledPos, invert, i, depth):
    global refPoint, darkest, brightest, success, fail
    set_color(i, 255, 255, 255)

    time.sleep(.15) # Needed to ensure frame with LED on is captured. 
    image = vid.read()[refPoint[0][1]:refPoint[1][1], refPoint[0][0]:refPoint[1][0]]
    if invert:
        image = cv2.flip(image, 1)

    # Get brightest spot in image
    orig = image.copy()
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    gray = cv2.GaussianBlur(gray, (41, 41), 0)
    (minVal, maxVal, minLoc, maxLoc) = cv2.minMaxLoc(gray)
    log("Current maxVal is "+str(maxVal))
    image = orig.copy()
    if maxVal >= darkest+((brightest-darkest)*.5):
        cv2.circle(image, maxLoc, 20, (0, 255, 0), 2) # Seen
        if depth:
            ledPos[i][2] = maxLoc # TODO: Use y coord to check position and detect errors
        else:
            ledPos[i] = ['SUCCESS', maxLoc, 'NONE']
        success += 1
    else:
        fail += 1
        cv2.circle(image, maxLoc, 20, (0, 0, 255), 2) # Not seen
        if depth:
            ledPos[i][0] = 'RECALIBRATE-Z'
            ledPos[i][2] = maxLoc
        else:
            ledPos[i] = ['RECALIBRATE', maxLoc, 'NONE']

    cv2.setWindowTitle(winname, 'LED index: '+str(i))
    cv2.imshow(winname, image)
    set_color(i, 0, 0, 0)

def scan(condition, invert, depth, ledPos): 
    global imageF
    for i in range(numLed):
        set_color(i, 0, 0, 0)
    for i in range(numLed): # TODO: Put this in a function
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break
        if condition:
            if ledPos[i][0] == condition:
                getBrightest(ledPos, invert, i, depth)
        else:
            getBrightest(ledPos, invert, i, depth)

def prompt(message):
    log(message)
    cv2.setWindowTitle(winname, "Rotate the jar, then press 'c'")
    while True:
        image = vid.read()[refPoint[0][1]:refPoint[1][1], refPoint[0][0]:refPoint[1][0]]
        cv2.imshow(winname, image)
        if cv2.waitKey(33) == ord('c'):
            log("Got key!")
            break

def postProcess(): # Normalize values, and invert Y since the origin is the top left in OpenCV.
    global ledPos
    yMax = -1000
    xMin, yMin, zMin = 1000

    for i in range(len(ledPos)-1): # Get max and mins
        xMin = min(ledPos[i][1][0], xMin)

        yMax = max(ledPos[i][1][1], yMax)
        yMin = min(ledPos[i][1][1], yMin)

        zMin = min(ledPos[i][2][0], zMin)

    for i in range(len(ledPos)-1): # Normalize values
        ledPos[i][1][0] = ledPos[i][1][0] - xMin
        ledPos[i][1][1] = ledPos[i][1][1] - yMin
        ledPos[i][2][0] = ledPos[i][2][0] - zMin

    for i in range(len(ledPos)-1): # Invert Y values
        yMid = yMax / 2
        currentY = ledPos[i][1][1]
        if currentY > yMid:
            ledPos[i][1][1] - (currentY - yMid)*2
        elif currentY < yMid:
            ledPos[i][1][1] + (yMid - currentY)*2
            

print("Starting calibration...")

cv2.namedWindow(winname) # Create a named window
# cv2.moveWindow(winname, 2040,30)  # Move it to be visible
cv2.setMouseCallback(winname, mouse_crop) # For cropping

# log("Please ensure the first LED is unobstructed, and the second LED is as obstructed as the LEDs will get.")
log("Brightest and darkest calibrating...")
vid = VideoCapture(camera_index) # Camera index 2 for external camera.
# vid.set(cv2.CAP_PROP_BUFFERSIZE, 0) # Only works on certain OpenCV backends
# for i in range(10): # Allow camera to adjust exposure before calibration
#     image = vid.read()

cropImg = vid.read()
# cv2.putText(img=cropImg, text='Please drag the mouse around just the jar for best calibration. Press any key to continue after.', org=(15, 20), fontFace=cv2.FONT_HERSHEY_TRIPLEX, fontScale=.7, color=(255, 0, 0),thickness=2)
cv2.setWindowTitle(winname, 'Drag the mouse around the container.')
cv2.imshow(winname, cropImg)
cv2.waitKey(0)

set_color(0, 255, 255, 255)
time.sleep(.5)
image = vid.read()[refPoint[0][1]:refPoint[1][1], refPoint[0][0]:refPoint[1][0]]
block = True
cv2.imshow(winname, image)
# cv2.waitKey(0)

orig = image.copy()
gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
gray = cv2.GaussianBlur(gray, (41, 41), 0)
(minVal, maxVal, minLoc, maxLoc) = cv2.minMaxLoc(gray)
log("brightest for first LED (unobstructed) is:"+str(maxVal))
brightest = maxVal
image = orig.copy()
cv2.circle(image, maxLoc, 20, (255, 0, 0), 2)
# cv2.putText(img=image, text='The LED should be circled.', org=(15, 20), fontFace=cv2.FONT_HERSHEY_TRIPLEX, fontScale=.7, color=(255, 0, 0),thickness=2)
cv2.setWindowTitle(winname, 'The LED should be circled.')
set_color(0, 0, 0, 0)
cv2.imshow(winname, image)
cv2.waitKey(0)


image = vid.read()[refPoint[0][1]:refPoint[1][1], refPoint[0][0]:refPoint[1][0]]

orig = image.copy()
gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
gray = cv2.GaussianBlur(gray, (41, 41), 0)
(minVal, maxVal, minLoc, maxLoc) = cv2.minMaxLoc(gray)
log("darkest for image is"+str(maxVal))
darkest = maxVal
image = orig.copy()
cv2.circle(image, maxLoc, 20, (255, 0, 0), 2)
# cv2.putText(img=image, text='The brightest non-LED spot should be circled', org=(15, 20), fontFace=cv2.FONT_HERSHEY_TRIPLEX, fontScale=.7, color=(255, 0, 0),thickness=2)
cv2.setWindowTitle(winname, 'Getting brightest and darkest spots...')
cv2.imshow(winname, image)

log("Running initial calibration...")
scan(False, False, False, ledPos)

log(ledPos)

for i in range(numLed):
    set_color(i, 0, 0, 0)

log(str(success)+" succesful calibrations, "+str(numLed-success)+" failed calibrations.")
success = 0 
fail = 0

prompt("Please rotate the jar 180 degrees.")
log("Running second calibration...")
scan('RECALIBRATE', True, False, ledPos)

if numLed-success > 0:
    log(str(success)+" succesful calibrations, "+str(fail)+" failed calibrations on second run. ", "w")
    log("Entering manual calibration mode!", "w")
    manual_calibrate(ledPos)
else:
    log(str(success)+" succesful calibrations. All LED's have been xy calibrated!")
print(ledPos)

log("Exited manual calibration mode, assuming succesfull XY calibration! Running depth calibration.")
log(ledPos)

success = 0 
fail = 0

prompt("Please rotate the jar 90 degrees.")
log("Running Z calibration...")
scan(False, False, True, ledPos)

log(ledPos)

if fail != 0:
    success = 0
    fail = 0
    prompt("Pleas rotate the jar 270 degrees.")
    log("Running second Z calibration...")
    scan('RECALIBRATE-Z', True, True, ledPos)
    if fail != 0:
        manual_calibrate(ledPos, True) # Depth mode

success = 0
fail = 0
conf = input("Post-process and write collected data?[y/N] ")
if conf.lower() == "y":
    postProcess()
    with open(ledPosFile, 'w') as f:
        json.dump(ledPos, f)
log("Done.")

#print(ledPos)
