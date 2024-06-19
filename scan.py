import yaml
import cv2
import paramiko

numLed = 50 #num of leds
vid = cv2.VideoCapture(0)
ledPos=[[0]*2]*50
#imageF=0
first = True

host = "192.168.86.248"
username = "pi"
password = "raspberry"

client = paramiko.client.SSHClient()
client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
client.connect(host, username=username, password=password)
#_stdin, _stdout,_stderr = client.exec_command("pwd")
#print(_stdout.read().decode())
#client.close()

for i in range(numLed):
#while True:
    global imageF
    #print('''sudo python3 -c "import board; import neopixel; pixels = neopixel.NeoPixel(board.D18, 50, pixel_order=neopixel.RGB); pixels.fill((0, 0, 0)); pixels['''+str(i)+'''] = (255, 255, 255); print('Done.')"''')
    _stdin, _stdout,_stderr = client.exec_command('''sudo python3 -c "import board; import neopixel; pixels = neopixel.NeoPixel(board.D18, 50, pixel_order=neopixel.RGB); pixels.fill((0, 0, 0)); pixels['''+str(i)+'''] = (255, 255, 255); print('Done.')"''')
    out = _stdout.read().decode()
    #if out != "Done.":
    #    print("Changing led failed! For reason: "+out)

    ret, image = vid.read()
    #cv2.imshow('frame', image
    orig = image.copy()
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    gray = cv2.GaussianBlur(gray, (41, 41), 0)
    (minVal, maxVal, minLoc, maxLoc) = cv2.minMaxLoc(gray)
    image = orig.copy()
    cv2.circle(image, maxLoc, 41, (255, 0, 0), 2)
    ledPos[i] = maxLoc
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break
    cv2.imshow("frame", image)
client.close()
conf = input("Write collected data?[y/N]")
if conf.lower() == "y":
    with open('ledPos.yml', 'w') as f:
        f.write(yaml.dump(ledPos))

#print(ledPos)
