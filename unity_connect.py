import socket
import struct
import traceback
import logging
import time
from receive_esp8266.esp8266_udp import set_color
from simpleLog import log

def sending_and_reciveing():
    s = socket.socket()
    socket.setdefaulttimeout(None)
    port = 60000
    s.bind(('127.0.0.1', port)) #local host
    s.listen(30)
    log("Socket waiting for connection...")
    while True:
        try:
            c, addr = s.accept() 
            bytes_received = c.recv(4000) 
            array_received = np.frombuffer(bytes_received, dtype=np.float32) #converting into float array

            nn_output = return_prediction(array_received) #Code here

            bytes_to_send = struct.pack('%sf' % len(nn_output), *nn_output) #converting float to byte
            c.sendall(bytes_to_send) #sending back
            c.close()
        except Exception as e:
            logging.error(traceback.format_exc())
            print("error")
            c.sendall(bytearray([]))
            c.close()
            break

sending_and_reciveing() 