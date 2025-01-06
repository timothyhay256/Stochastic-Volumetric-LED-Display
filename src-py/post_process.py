import json
input_file = open("ledPosJarOne.json")
ledPos = json.load(input_file)

print(ledPos)

def postProcess(): # Normalize values, and invert Y since the origin is the top left in OpenCV.
    global ledPos
    yMax = -1000
    zMax = -1000
    xMin = 1000
    yMin = 1000
    zMin = 1000

    for i in range(len(ledPos)-1): # Get max and mins
        xMin = min(ledPos[i][1][0], xMin)

        yMax = max(ledPos[i][1][1], yMax)
        yMin = min(ledPos[i][1][1], yMin)

        zMin = min(ledPos[i][2][0], zMin)
        zMax = max(ledPos[i][2][0], zMax)

    for i in range(len(ledPos)-1): # Normalize values
        ledPos[i][1][0] = ledPos[i][1][0] - xMin
        ledPos[i][1][1] = ledPos[i][1][1] - yMin
        ledPos[i][2][0] = ledPos[i][2][0] - zMin

    for i in range(len(ledPos)-1): # "Rotate" 180 degrees upside down to account for OpenCV origin being top left
        yMid = yMax / 2 # Invert Y
        currentY = ledPos[i][1][1]
        if currentY > yMid:
            ledPos[i][1][1] = yMid - (currentY - yMid)
        elif currentY < yMid:
            ledPos[i][1][1] = yMid + (yMid - currentY)

        zMid = zMax / 2 # Invert X
        currentZ = ledPos[i][2][0]
        if currentZ > zMid:
            ledPos[i][2][0] = zMid - (currentZ - zMid)
        elif currentY < yMid:
            ledPos[i][2][0] = zMid + (zMid - currentZ)

postProcess()
print(ledPos)

with open('postProcessLedPos.json', 'w') as f:
    json.dump(ledPos, f)