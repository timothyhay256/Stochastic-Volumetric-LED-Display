/*
This script will use one CPU core to accept led commands via serial,
 and the other to calculate and send the current angle of the jar (using MPU6050)
  over UDP to a target server.
Gets angle measurements using Jeff Rowbergs code from here: https://github.com/jrowberg/i2cdevlib
*/
#include <Arduino.h>
#include <FastLED.h> 
#include <WiFi.h>
#include <WiFiUdp.h>
/*
Begin MPU6050 Code
*/
#include "I2Cdev.h"
#include "MPU6050_6Axis_MotionApps20.h"

MPU6050 mpu;

#define INTERRUPT_PIN 2

volatile bool mpuInterrupt = false; // Interrupt flag
bool dmpReady = false;              // DMP status flag
uint8_t mpuIntStatus;               // MPU interrupt status
uint8_t devStatus;                  // Device status
uint16_t packetSize;                // DMP packet size
uint8_t fifoBuffer[64];             // FIFO buffer

Quaternion q;                       // Quaternion data container
VectorFloat gravity;                // Gravity vector
float ypr[3];                       // Yaw/Pitch/Roll angles

void dmpDataReady() {
    mpuInterrupt = true;
}
/*
End MPU6050 code
*/
TaskHandle_t ledCommands; // Task for multicore

// Set variables below!
const char* ssid = "STN"; // Network SSID
const char* password = "88bb6b7054"; // Network password
const char* udpTarget = "192.168.86.111"; // Where should I send gyroscope data?
const int udpPort = 5011; // On what port should I send gyroscope data?

#define LED_COUNT 50 // How many LEDs?
#define LED_PIN 27 // On which pin?
#define BAUD_RATE 115200 // What baudrate should I use

// When using a MPU6050 gyroscope, then you need to set the offsets. These are printed to serial on boot, in the following format:
// >.....>.......ignoreMe,  ignoreMe,   zAccelOffset,   xGyroOffset,    yGyroOffset,    zGyroOffset
const int xGyroOffset = 17; 
const int yGyroOffset = 88; 
const int zGyroOffset = 2; 
const int zAccelOffset = 1010; 
// Set variables above!

bool sendBack = false; // Should I send back what instructions I just carried out? For debugging.
WiFiUDP udp;

CRGB leds[LED_COUNT];

int n, r, g, b;

void receiveCommands(void * parameter);

void setup() {
  /*
  MPU6050 Setup Begin
  */
  Wire.begin();
  Wire.setClock(400000); // Optional, adjust as necessary
  Serial.begin(BAUD_RATE);
  while (!Serial);

  pinMode(INTERRUPT_PIN, INPUT);

  Serial.println(F("Initializing I2C devices..."));
  mpu.initialize();

  Serial.println(F("Testing device connections..."));
  Serial.println(mpu.testConnection() ? F("MPU6050 connection successful") : F("MPU6050 connection failed"));

  Serial.println(F("Initializing DMP..."));
  devStatus = mpu.dmpInitialize();

  mpu.setXGyroOffset(xGyroOffset);
  mpu.setYGyroOffset(yGyroOffset);
  mpu.setZGyroOffset(zGyroOffset);
  mpu.setZAccelOffset(zAccelOffset); // Adjust as necessary

  if (devStatus == 0) {
      mpu.CalibrateAccel(6);
      mpu.CalibrateGyro(6);
      mpu.PrintActiveOffsets();

      Serial.println(F("Enabling DMP..."));
      mpu.setDMPEnabled(true);

      Serial.print(F("Enabling interrupt detection (Arduino external interrupt "));
      Serial.print(digitalPinToInterrupt(INTERRUPT_PIN));
      Serial.println(F(")..."));
      attachInterrupt(digitalPinToInterrupt(INTERRUPT_PIN), dmpDataReady, RISING);
      mpuIntStatus = mpu.getIntStatus();

      Serial.println(F("DMP ready! Waiting for first interrupt..."));
      dmpReady = true;

      packetSize = mpu.dmpGetFIFOPacketSize();
  } else {
      Serial.print(F("DMP Initialization failed (code "));
      Serial.print(devStatus);
      Serial.println(F(")"));
  }

  /*
  MPU6050 Setup End
  */

  //FastLED setup 
  FastLED.addLeds<WS2811, 27, RGB>(leds, 50);
  FastLED.clear();
  FastLED.show(); 

  xTaskCreatePinnedToCore( // Start serial receive process 
      receiveCommands, /* Function to implement the task */
      "ledCommands", /* Name of the task */
      10000,  /* Stack size in words */
      NULL,  /* Task input parameter */
      0,  /* Priority of the task */
      &ledCommands,  /* Task handle. */
  0); /* Core where the task should run */

  // Wifi Setup
  Serial.print(F("Connecting to WiFi "));
  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
      delay(1000);
      Serial.print(F("."));
  }
  Serial.println(F(" connected!"));

  // Initialize UDP
  udp.begin(udpPort);
}

void loop() {
  if (!dmpReady) return;
  delay(50);
  if (mpu.dmpGetCurrentFIFOPacket(fifoBuffer)) {
      mpu.dmpGetQuaternion(&q, fifoBuffer);

      udp.beginPacket(udpTarget, udpPort);
      udp.print(q.w);
      udp.print(",");
      udp.print(q.x);
      udp.print(",");
      udp.print(q.y);
      udp.print(",");
      udp.println(q.z);
      udp.endPacket();
  }
}

void receiveCommands(void * parameter) {
  for (;;) {
    if (Serial.available() >= 2) { // Wait for start of packet bytes to be available
      if (Serial.read() == 0xFF) {
        if (Serial.read() == 0xBB) { // SOP bytes confirmed
          n = Serial.read(); // Read the first byte and assign it to n
          r = Serial.read(); // Read the second byte and assign it to r
          g = Serial.read(); // Read the third byte and assign it to g
          b = Serial.read(); // Read the fourth byte and assign it to b

          // Set the color of the specified LED
          leds[n] = CRGB(r, g, b);
          FastLED.show();
          Serial.write(0x01); // Send a single byte (acknowledgment)
        }
      }
    }
  }
}
