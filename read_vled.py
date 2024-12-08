from led_manager import set_color
from simpleLog import log
import time

vledFile = "temp.vled"
'''
vled format is as follows:
Color command:  index|r|g|b
Clear command:  Eindex (Note the clear command is not just index|0|0|0 to make the file more human readable. Both will work.)
Wait command:   Ttime 
'''

sentPacketsSecond = 0

while True:
    log("Reading "+vledFile)
    start = time.time()
    with open(vledFile, "r") as file:
        for line in file:
            line = line.strip()
            if "E" in line:
                # log("Clearing index "+line.strip("E"))
                set_color(line.strip("E"), 0, 0, 0) # Clear the color
                sentPacketsSecond += 1
            elif "|" in line:
                data = line.split("|")
                # log("Setting index "+data[0]+" with R: "+data[1]+" G: "+data[2]+" B: "+data[3])
                set_color(data[0], data[1], data[2], data[3]) # Set index with color
                sentPacketsSecond += 1
            elif "T" in line:
                # log("Sleeping for "+str(float(line.split(":")[1])))
                time.sleep(float(line.split(":")[1]))
            if time.time() - start >= 1:
                print(str(sentPacketsSecond)+" packets per "+str(round(time.time() - start, 0)).strip(".0")+" second.")
                # print(str(sentPacketsSecond*12)+" bytes per second.")
                start = time.time()
                sentPacketsSecond = 0
