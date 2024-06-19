import serial

# Open serial port
ser = serial.Serial('/dev/ttyUSB0', 115200)  # Replace '/dev/ttyACM0' with the appropriate port on your system

# Function to send 4 bytes and receive acknowledgment
def send_data_and_receive_ack(n, r, g, b):
    # Send 4 bytes to Arduino
    ser.write(bytes([n, r, g, b]))
    # Receive acknowledgment from Arduino
    ack = ser.read()
    return ack

# Example usage
n = 10
r = 20
g = 30
b = 40
acknowledgment = send_data_and_receive_ack(n, r, g, b)
print("Received acknowledgment:", acknowledgment)
