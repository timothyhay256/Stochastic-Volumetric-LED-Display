Import("env")
import glob
import sys
import threading
from base64 import b64decode
from threading import Thread

import serial
from platformio import util

print("\nextra script info:")
print("  Monitor speed:", env.GetProjectOption("monitor_speed"))
print("  Simultaneous upload ports:", env.GetProjectOption("simultaneous_upload_ports"))
print("  Current build targets:", list(map(str, BUILD_TARGETS)))


returnCodes = []  # list of tuples storing com port and upload result


def run(port):
    env.Replace(UPLOAD_PORT=port)
    for i in range(0, 3):  # try up to 3 times
        if i > 0:
            env.Replace(UPLOAD_SPEED="115200")  # try slowing down baud
        command = (
            env.subst("$UPLOADCMD")
            + " "
            + env.subst("$BUILD_DIR")
            + "/"
            + env.subst("$PROGNAME")
            + ".bin"
        )

        errorCode = env.Execute(command)
        if errorCode == 0:
            returnCodes.append((port, errorCode))
            return

    returnCodes.append((port, errorCode))


def after_build(source, target, env):
    print("Build finished\n\nmulti_esptool.py script:")
    simultaneous_upload_ports = env.GetProjectOption("simultaneous_upload_ports")
    # print(f"Simultaneous upload ports: {simultaneous_upload_ports}")
    if simultaneous_upload_ports != None:  # check if defined
        threads = []
        ports = simultaneous_upload_ports
        print(f"Given port(s): {ports}")
        if str(ports) == "AUTO":
            print("getting serial ports...")
            ports = util.get_serial_ports()
            print("I got these ports:", ports)
            # Windows
            if "port': 'COM" in str(ports):
                print("-> looks like we're on a Windows system...")
                port_list = []
                for port in ports:
                    if "USB VID:PID=1A86:" in str(port["hwid"]):
                        port_list.append(port["port"])
                print("-> filtered ports for VID=1A86: " + str(port_list))
            else:
                # macOS
                if "/dev/cu.usbserial" in str(ports):
                    print("-> looks like we're on an Apple macOS...")
                    port_list = []
                    for port in ports:
                        if "/dev/cu.usbserial" in str(port):
                            port_list.append(port["port"])
                    print("-> filtered ports for /dev/cu.usbserial: " + str(port_list))
                # other OS
                else:
                    port_list = []
                    for port in ports:
                        port_list.append(port["port"])
                    print("-> I found these ports:", port_list)
        else:
            # split configuration string from platformio.ini into a list of ports
            port_list = str(ports).split(",")
        print("I'll try uploading to ", port_list)
        for port in port_list:
            # thread = Thread(target=run, args=(port,))
            thread = Thread(target=run, args=(str(port).replace(" ", ""),)) # remove blanks
            thread.start()
            threads.append(thread)
        for thread in threads:
            thread.join()  # wait for all threads to finish
        encounteredError = False
        returnCodes.sort(key=lambda code: code[0])
        for code in returnCodes:
            if code[1] == 0:
                print(f"{code[0]} Uploaded Successfully")
            elif code[1] == 1:
                print(f"{code[0]}: Encountered Exception, Check serial port")
                encounteredError = True
            elif code[1] == 2:
                print(f"{code[0]}: Encountered Fatal Error")
                encounteredError = True
        if encounteredError:
            Exit(1)
        Exit(0)
    else:
        print("No Simultaneous Upload Ports Defined")


env.AddPreAction("upload", after_build)
