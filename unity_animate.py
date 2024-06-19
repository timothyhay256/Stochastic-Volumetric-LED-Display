import socket
import time
import subprocess

outFile = 'data' # Gets headset data 
optionFile = '/home/jamesh/Brain/options' # Gets current threshold and punishment mode

def rwOptions(write=False, threshold=0, punishAttention=0, punishMeditation=0, punishWith=3, wantOption=0): # Option 0 is threshold, 1 is punishAttention, 2 is punishMeditation, 3 is punishWith
    if write == False:
        # bugLog("Options are being read from!")
        options = open(optionFile, 'r')
        if wantOption == 0:
            return(options.read().split(",")[0])
        elif wantOption == 1:
            return(options.read().split(",")[1])
        elif wantOption == 2:
            return(options.read().split(",")[2])
        elif wantOption == 3:
            return(options.read().split(",")[3])
    elif write:
        bugLog("Options are being written to!")
        options = open(optionFile, 'w')
        options.write(str(threshold)+","+str(punishAttention)+","+str(punishMeditation)+","+str(punishWith))
        options.flush()
        options.close()

def send_data():
    data = [0, 0, 0, 0, 0]

    host, port = "127.0.0.1", 5003
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect((host, port))

    while True:
        time.sleep(.5)
        line = str(subprocess.check_output(['tail', '-1', '/home/jamesh/Brain/'+outFile]).decode('utf-8')).strip("\n").split(",")
        threshold = rwOptions(wantOption=0)
        punishAttention = rwOptions(wantOption=1)
        punishMeditation = rwOptions(wantOption=2)

        data[0] = line[2] # Current attention
        data[1] = line[3] # Current meditation
        data[2] = threshold
        data[3] = punishAttention
        data[4] = punishMeditation

        dataString = ','.join(map(str, data))
        print(dataString)

        sock.sendall(dataString.encode("UTF-8"))
        receivedData = sock.recv(1024).decode("UTF-8") #receiveing data in Byte fron C#, and converting it to String
        print(receivedData)
        if str(receivedData) == "ack":
            pass
        else:
            log("Did not get response from Unity! Exiting.", "e")
            exit(1) # This should be fine because there *should* be no packet loss locally


# send_data()

# while True:
#     line = str(subprocess.check_output(['tail', '-1', '/home/jamesh/Brain/'+outFile]).decode('utf-8')).strip("\n").split(",")

#     threshold = rwOptions(wantOption=0)
#     punishAttention = rwOptions(wantOption=1)
#     punishMeditation = rwOptions(wantOption=2)
#     print(line)
#     print(threshold)
#     print(punishAttention)
#     print(punishMeditation)
#     print("\n")
#     time.sleep(1)