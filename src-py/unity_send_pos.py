# from led_manager import set_color
from simpleLog import log
import socket
import select
import time
import json

yMod = 0 # Add how much to Y to account for inversion
def send_pos(host, port, posFile, yMod=yMod, scale=.08):
    # host, port = "127.0.0.1", 5001
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect((host, port))
    # sock.setblocking(0)

    f = open(posFile)
    ledPos = json.load(f)

    for i in range(int(len(ledPos)-1)):
        # print(i)
        # print(int(len(ledPos)-1))
        setPos = [0, 0, 0] # Will be interpreted as Vector3
        setPos[0] = (ledPos[i][1][0])*scale
        setPos[1] = ((ledPos[i][1][1])*scale) + yMod# TODO: Invert 
        setPos[2] = (ledPos[i][2][0])*scale

        # print(setPos)

        posString = ','.join(map(str, setPos))
        # print(posString)
        sock.sendall(posString.encode("UTF-8")) #Converting string to Byte, and sending it to C#
        receivedData = sock.recv(1024).decode("UTF-8") #receiveing data in Byte fron C#, and converting it to String
        # print(receivedData)
        if str(receivedData) == "ack":
            pass
        else:
            log("Did not get response from Unity! Exiting.", "e")
            exit(1)
    sock.sendall("END".encode("UTF-8"))
    log("Finished sending positions to Unity!")
# send_pos()
    # startPos = [0, 0, 0] #Vector3   x = 0, y = 0, z = 0
    # while True:
    #     time.sleep(0.5) #sleep 0.5 sec
    #     startPos[0] +=1 #increase x by one
    #     posString = ','.join(map(str, startPos)) #Converting Vector3 to a string, example "0,0,0"
    #     print(posString)

    #     sock.sendall(posString.encode("UTF-8")) #Converting string to Byte, and sending it to C#
    #     receivedData = sock.recv(1024).decode("UTF-8") #receiveing data in Byte fron C#, and converting it to String
    #     print(receivedData)
# send_pos()