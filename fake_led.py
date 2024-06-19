# from receive_esp8266.esp8266_udp import set_color
from simpleLog import log
import socket

# Set up UDP socket
UDP_IP = "127.0.0.1"
UDP_PORT = 8888


def main():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((UDP_IP, UDP_PORT))

    print("UDP server listening on {}:{}".format(UDP_IP, UDP_PORT))

    # Receive data continuously
    while True:
        data, addr = sock.recvfrom(1024)  # buffer size is 1024 bytes
        data = data.decode()
        # print(data)

        sock.sendto(b"ack", addr)