import socket # TODO: Test ESP commands, write ESP command read code, and add ability for serial.
import time
import select
import serial
# import struct

# host = "192.168.1.105"
host = "127.0.0.1"
port = 8888
failures = 0
failureLimit = 10 # Exit if this many concurrent timeouts/failures occur
record_data = False # Record the data to disk?
record_esp_data = True # Record a bytestring for the ESP8266?
data_file = 'udpOut.vled'
esp_data_file = 'udpOut.bvled' # Byte vled.
esp_data = []

communicate_mode = 2 # 1 For UDP, 2 for serial

serial_port = '/dev/ttyUSB0'
serial_baudrate = 921600

if communicate_mode == 1:
    print("Using UDP!")
    print("Going to fire packets at "+str(host)+":"+str(port))
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
elif communicate_mode == 2:
    print("Using serial!")
    print("Going to fire instructions at "+serial_port+" with baudrate of "+str(serial_baudrate))
    ser = serial.Serial(serial_port, serial_baudrate, timeout=.1)

if record_data:
    log("Writing packets to "+data_file)
    df = open(data_file, 'w')
start = time.time() # Records time between calls for disk

def set_color(n, r, g, b):
    global failures, failureLimit, esp_data, start
    n, r, g, b = str(n), str(r), str(g), str(b) #ESP will take the final string and set values according to their position. As such, currently you cannot do more than 99 pixels.
    if len(n) == 1:
        n = "00" + n
    elif len(n) == 2:
        n = "0"+n
    for i in range(3-len(r)):
        r = "0" + r
    for i in range(3 - len(g)):
        g = "0" + g
    for i in range(3-len(b)):
        b = "0" + b
    # print(n+r+g+b)
    if record_data:
        end = time.time()
        if end-start >= .001:
            df.write("T:"+str(end-start)+"\n")
        df.write(n+"|"+r+"|"+g+"|"+b+"\n")
        start = time.time()
    elif record_esp_data:
        # print(esp_data)
        end = time.time()
        timeElapsed = end-start
        if int((timeElapsed)*1000) >= 1: # Only count time greater than a ms
            esp_data.extend([1] * 4) # Indicates a timing instruction. See idea.md for more info.
            # print(int((timeElapsed)*1000))
            timeElapsed = int((timeElapsed)*1000) # Convert to ms.
            while timeElapsed > 255: # Add any overflow
                esp_data.append(255) 
                esp_data.extend([1] * 4)
                timeElapsed -= 255
            if timeElapsed > 0:
                esp_data.append(timeElapsed)
            # print(esp_data)
        esp_data.append(int(n))
        esp_data.append(int(r))
        esp_data.append(int(g))
        esp_data.append(int(b))
        start = time.time()
    if communicate_mode == 1:
        sock.sendto((n+r+g+b).encode(), (host, port))
        # print(n+r+g+b)
        ready = select.select([sock], [], [], .1) # Wait 100 ms for response in UDP
        if (ready[0]):
            data, addr = sock.recvfrom(1) # Blocks function until confirmed new pixel color
            data = data.decode("utf-8")
            if data == "BAD":
                failures += 1
                print("WARNING: ESP reported a malformed packet!")
            else:
                failures = 0
        else:
            if failures >= failureLimit:
                print("ERROR: Too many failures sending packets! Giving up.")
                exit(1)
            failures += 1
            print("Reached packet timeout! Re-sending but not waiting for response!")
            sock.sendto((n+r+g+b).encode(), (host, port))
    elif communicate_mode == 2:
        byte_string = bytes([int(n), int(r), int(g), int(b)])
        ser.write(byte_string)
        ack = ser.read()


def save_esp_dat(esp_data_file):
    global esp_data
    # print(esp_data)
    byte_array = bytes(esp_data)
    hex_string = ', 0x'.join(['{:02x}'.format(b) for b in byte_array])
    # print(hex_string)
    df = open(esp_data_file, 'w')
    df.write(str(hex_string))
    df.flush()
    df.close()
for i in range(200):
    set_color(i, 0, 0, 0)
'''
start = time.time()
for i in range(50):
    set_color(i, 0, 0, 0)
#set_color(1, 2, 3, 4)
#set_color(10, 20, 30, 40)
#set_color(20, 255, 255, 255)
for i in range(50):
    set_color(i, 255, 255, 255)
print(f'Time: {time.time() - start}')
#set_color(-1, 50, 255, 65) # Fill the strip
'''