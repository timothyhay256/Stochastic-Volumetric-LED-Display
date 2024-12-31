from led_manager import set_color, set_communicate_mode
from simpleLog import log
import socket
import time # TODO: Does writing to disk slow stuff down?

# record_data = True # Record the data to disk?
data_file = 'unityOut.vled'

def get_events(UDP_IP, UDP_PORT, communicate_mode, serial_port='', serial_baudrate='', led_host='', led_port='', record_data=False, data_file='unityOut.vled'):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((UDP_IP, UDP_PORT))

    log("UDP server listening on {}:{}".format(UDP_IP, UDP_PORT))

    if record_data:
        log("Writing animation data to "+data_file)
        df = open(data_file, 'w')

    if communicate_mode == 1 and len(led_host) != 0 and led_port != 0:
        log("Setting UDP as communicateion mode!")
        set_communicate_mode(1, udp_host=led_host, udp_port=led_port)
    elif communicate_mode == 2 and len(serial_port) != 0 and serial_baudrate != 0:
        log("Setting serial as communication mode!")
        set_communicate_mode(2, serial_port=serial_port, serial_baudrate=serial_baudrate)
    else:
        log("No valid options provided to get_events for communication mode, using default communication defined in led_manager!", "w")
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