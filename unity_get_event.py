from receive_esp8266.esp8266_udp import set_color
from simpleLog import log
import socket
import time # TODO: Does writing to disk slow stuff down?

# Set up UDP socket
UDP_IP = "127.0.0.1"
UDP_PORT = 5002

record_data = True # Record the data to disk?
data_file = 'unityOut.vled'

def get_events():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((UDP_IP, UDP_PORT))

    print("UDP server listening on {}:{}".format(UDP_IP, UDP_PORT))

    if record_data:
        log("Writing animation data to "+data_file)
        df = open(data_file, 'w')

    # Receive data continuously
    while True:
        if record_data:
            start = time.time() # Measure time for each instruction for disk
        data, addr = sock.recvfrom(1024)  # buffer size is 1024 bytes
        data = data.decode()
        if record_data:
                end = time.time()
                # print(data)
                df.write(data)
                df.write("\n")
                if end-start >= .001:
                    df.write("T:"+str(end-start)+"\n")
        if "E" in data:
            # log("Clearing index "+data.strip("E"))
            set_color(data.strip("E"), 0, 0, 0) # Clear the color
        elif "|" in data:
            data = data.split("|")
            # log("Setting index "+data[0]+" with R: "+data[1]+" G: "+data[2]+" B: "+data[3])
            set_color(data[0], data[1], data[2], data[3])

# get_events()