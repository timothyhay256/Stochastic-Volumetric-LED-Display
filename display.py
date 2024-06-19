#2d cross test. Runs below any wrappers, and works of raw data.
import json
from receive_esp8266.esp8266_udp import set_color
import cv2

threshold = 12
f = open('ledPos.json')
ledPos = json.load(f)
triggered=[[0]*2]*50
postCal = True
center = True

#ledPos = np.array(ledPos)
lowest = 100000
def main(x, y):
    global lowest, center, postCal, triggered, ledPos, f, threshold
    def find_closest_to_median(new_int, int_list=[]):
        int_list.append(new_int)
        int_list.sort()
        n = len(int_list)
        if n % 2 == 1:
            median = int_list[n//2]
        else:
            median = (int_list[n//2-1] + int_list[n//2]) / 2
        closest = int_list[0]
        for num in int_list:
            if abs(num - median) < abs(closest - median):
                closest = num
        return closest

    for i in range(len(ledPos)): #This is where the "focal point" is set
            pos = ledPos[i]
            if x:
                pos = pos[0]
            if y:
                pos = pos[1]
            if center:
                    lowest = find_closest_to_median(pos)
            else:
                    if pos < lowest:
                            lowest = pos
    print("Lowest is: "+str(lowest))
    thresh = (lowest/100)*threshold
    print("Thresh is: "+str(thresh))
    for i in range(len(ledPos)):
            pos = ledPos[i]
            if x:
                pos = pos[0]
            if y:
                pos = pos[1]
            #print("Pos is: "+str(pos)+" while I is: "+str(i))
            if lowest-thresh <= pos <= thresh+lowest:
                    print("Pos: "+str(pos)+" Within thresholds: "+str(lowest-thresh)+"-"+str(thresh+lowest))
                    #print(pos)
                    print("Setting color at index "+str(i))
                    if x:
                        print("Setting index: "+str(i)+" with x value of: "+str(pos))
                    if y:
                        print("Setting index: " + str(i) + " with y value of: " + str(pos))
                    set_color(i, 255, 55, 255)
                    if postCal:
                            print("Setting triggered["+str(i)+"] to "+str(ledPos[i]))
                            triggered[i] = ledPos[i]
    if postCal:
            #print("Value of triggered is "+str(triggered))\
            vid = cv2.VideoCapture(2)
            ret, frame = vid.read()
            vid.release()
            for i in range(len(triggered)):
                    xy = triggered[i]
                    if xy[0] == 0 and xy[1] == 0:
                            pass
                    else:
                            print("Drawing circle at " + str(xy))
                            cv2.circle(frame, xy, 20, (255, 0, 0), 2)
            if x:
                winname = "Calibration check - x"
            if y:
                winname = "Calibration check - y"
            cv2.namedWindow(winname)        # Create a named window
            cv2.moveWindow(winname, 2040,30)  # Move it to (40,30)
            while True:
                    cv2.imshow(winname, frame)
                    if cv2.waitKey(1) & 0xFF == ord('q'):
                            break
main(x=True, y=False)

main(x=False, y=True)