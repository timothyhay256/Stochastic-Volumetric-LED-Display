import socket # TODO: Test ESP commands, write ESP command read code, and add ability for serial.
import time # TODO: Export on command instead of every time to avoid overfilling data
import select
import serial
import os.path
# import struct

# User definied variables, set settings here!
communicate_mode = 2 # What mode to use for communication with LEDs? 1 for UDP, 2 for serial.

host = "192.168.86.124" # If using UDP, what is the ESPs IP address? This is printed to serial when the ESP starts up.
port = 8888 # Don't change unless you changed this in the ESPs script.

serial_port = '/dev/ttyUSB0' # If using serial, what is the serial port of the ESP? Whatever is listed when you run `ls /dev | grep USB` is probably the right port.
serial_baudrate = 921600 # Don't change unless you changed this in ESPs script.

record_data = True # (Record mode) Record the data to disk?
record_esp_data = False # (Record mode) Record a bytestring for the ESP?
unityControlRecording = True # Should Unity control when to record animations and what mode to record? (Presence of /tmp/start_animate and /tmp/start_animate_byte)
data_file = 'udp.vled' # If recording a non bytestring, what file should it be stored in?
esp_data_file = 'udpOut.bvled' # If recording a bytestring, what file should it be stored in? (save_esp_dat MUST be called or no data will be saved. TODO: change that)

numLed = 0 # Optional, set to the number of LEDs if you wish to clear them on init
# End user defined variables

failureLimit = 15 # Exit if this many concurrent timeouts/failures occur
sleepOnFail = 10 # If non zero, sleep for n seconds on exit condition, otherwise, fail on exit condition
printSendBack = False # Print what the ESP sends back when using serial? Used for debugging

failures = 0
esp_data = []
firstRunRecord = True 

def set_communicate_mode(communicate_mode, serial_port='', serial_baudrate='', udp_host='', udp_port=8888):
    global host, port, ser, sock
    if communicate_mode == 1:
        print("Using UDP!")
        print("Going to fire packets at "+str(udp_host)+":"+str(udp_port))
        host = udp_host
        port = udp_port
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    elif communicate_mode == 2:
        print("Using serial!")
        print("Going to fire instructions at "+serial_port+" with baudrate of "+str(serial_baudrate))
        ser = serial.Serial(serial_port, serial_baudrate, timeout=10)

set_communicate_mode(communicate_mode, serial_port, serial_baudrate, host)

if record_data:
    print("Writing packets to "+data_file+" when activated.")
    df = open(data_file, 'w')
elif record_esp_data:
    print("Writing packets as bytes to "+esp_data_file+" when activated.")
    df = open(esp_data_file, 'w')
# start = time.time() # Records time between calls for disk

def save_esp_dat(esp_data_file):
    global esp_data
    # print(esp_data)
    byte_array = bytes(esp_data)
    hex_string = ', 0x'.join(['{:02x}'.format(b) for b in byte_array])
    # print(hex_string)
    df = open(esp_data_file, 'w')
    df.write("0x")
    df.write(str(hex_string))
    df.flush()
    df.close()

def set_color(n, r, g, b):
    global failures, failureLimit, esp_data, start, firstRunRecord, sock
    conf_byte_failures = 0
    n, r, g, b = str(n), str(r), str(g), str(b) #ESP will take the final string and set values according to their position. As such, currently you cannot do more than 99 pixels.
    # print(n+r+g+b)
    if unityControlRecording:
        record_data = os.path.isfile("/tmp/start_animate")
        record_esp_data = os.path.isfile("/tmp/start_animate_byte")
        if firstRunRecord:
            if record_data or record_esp_data:
                start = time.time() # Records time between calls for disk
                firstRunRecord = False;
    if record_data:
        end = time.time()
        if end-start >= .001:
            df.write("T:"+str(end-start)+"\n")
        df.write(n+"|"+r+"|"+g+"|"+b+"\n")
        df.flush()
        start = time.time()
    elif record_esp_data:
        # print(esp_data)
        end = time.time()
        timeElapsed = end-start
        
        if int((timeElapsed)*1000) >= 1: # Only count time greater than a ms
            # print(int((timeElapsed)*1000))
            timeElapsed = int((timeElapsed)*1000) # Convert to ms.
            while timeElapsed > 255: # Add any overflow
                print("Overflow! timeElapsed is "+str(timeElapsed))
                for i in range(1,5):
                    esp_data.append(i)
                esp_data.append(255) 
                timeElapsed -= 255
            if timeElapsed > 0:
                print("Not overflow")
                for i in range(1,5): # Indicates a timing instruction, as it is unlikely that LED 1 will be set to 2,3,4 (r,g,b) 
                    esp_data.append(i) 
                esp_data.append(timeElapsed)
            # print(esp_data)
        if int(n) == 1 and int(r) == 2 and int(g) == 3 and int(b) == 4: # This is the same format as a timing instruction, which if misinterpreted would completely fuck up the loop
            print("WARNING: Modifying a instruction by 1 to prevent parsing error!") 
            r = 3
        esp_data.append(int(n))
        esp_data.append(int(r))
        esp_data.append(int(g))
        esp_data.append(int(b))
        start = time.time()
    if os.path.isfile("/tmp/export_byte"):
        os.remove("/tmp/export_byte")
        print("Exporting data!")
        save_esp_dat(esp_data_file)

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
                if sleepOnFail == 0:
                    print("ERROR: Too many failures sending packets! Giving up.")
                    exit(1)
                else:
                    print("WARNING: Too many timeouts trying to connect, retrying in "+str(sleepOnFail)+" seconds.")
                    time.sleep(sleepOnFail)
                    failures = 0
            failures += 1
            print("Reached packet timeout! Re-sending but not waiting for response!")
            sock.sendto((n+r+g+b).encode(), (host, port))
    elif communicate_mode == 2:
        # print(n+r+g+b)

        # ser.write(b'\xFF\xBB')
        byte_string = b'\xFF\xBB'+bytes([int(n), int(r), int(g), int(b)]) # \xFF\xBB are SOP bytes
        ser.write(byte_string)
        if printSendBack:
            print(ser.readline().decode().strip())
        else:
            while ser.read() != b'\x01':
                conf_byte_failures += 1
                # if conf_byte_failures == 50:
                    # print("Did not get confirmation byte after five attempts, continuing anyway!")
                    # break

for i in range(numLed):
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