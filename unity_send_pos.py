# from receive_esp8266.esp8266_udp import set_color
from simpleLog import log
import socket
import time
import json

yMod = 22.6 # Add how much to Y to account for inversion
def send_pos():
    host, port = "127.0.0.1", 5001
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect((host, port))

    f = open('ledPos.json')
    ledPos = json.load(f)

    scale = .08 # Make positions less extreme to make them usable in Unity

    for i in range(len(ledPos)-1):
        print(i)
        setPos = [0, 0, 0] # Will be interpreted as Vector3
        setPos[0] = (ledPos[i][1][0])*scale
        setPos[1] = ((ledPos[i][1][1])*scale*-1) + yMod# TODO: Invert 
        setPos[2] = (ledPos[i][2][0])*scale

        print(setPos)

        posString = ','.join(map(str, setPos))
        sock.sendall(posString.encode("UTF-8")) #Converting string to Byte, and sending it to C#
        receivedData = sock.recv(1024).decode("UTF-8") #receiveing data in Byte fron C#, and converting it to String
        print(receivedData)
        if str(receivedData) == "ack":
            pass
        else:
            log("Did not get response from Unity! Exiting.", "e")
            exit(1)
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