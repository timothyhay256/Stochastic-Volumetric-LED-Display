// Remotely control Neopixel over WiFi - UDP and quicker buffers
#include <FastLED.h>
#include <ESP8266WiFi.h>
#include <WiFiUdp.h>

#define LED_PIN    3
#define LED_COUNT 150
#define LED_TYPE NEOPIXEL

CRGB red(255, 0, 0);
CRGB green(0, 255, 0);
CRGB clear(0);

int port = 8888;
const char *ssid = "SSID";
const char *password = "PASSWORD";

WiFiUDP UDP;
char packet[12];
char reply[] = "A";
char reply_bad[] = "BAD";

CRGB leds[LED_COUNT];

void setup() {
Serial.begin(115200);
  Serial.println("Serial Begin");

  FastLED.addLeds<LED_TYPE, LED_PIN>(leds, LED_COUNT);
  // FastLED.setBrightness(255);
  FastLED.clear();

  WiFi.begin(ssid, password);
  WiFi.setSleep(false);

  Serial.print("Connecting to ");
  Serial.print(ssid);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
    leds[0] = red;
    FastLED.show();
    delay(500);
    leds[0] = clear;
    FastLED.show();
  }
  leds[0] = green;
  FastLED.show();
  Serial.println(" \nConnected!");
  Serial.print("IP: ");
  Serial.println(WiFi.localIP());
  Serial.print("Port: ");
  Serial.println(port);
  UDP.begin(port);
  Serial.println("Listening for packets...");
}

void loop() {

  // If packet received...
  int packetSize = UDP.parsePacket();
  if (packetSize) {
    //Serial.print("Received packet! Size: ");
    //Serial.println(packetSize);
    int len = UDP.read(packet, 12);

    if (len > 0)
    {
      packet[len] = '\0';
    }
      //Serial.print("Packet received: ");
      //Serial.println(packet);
    String packetStr(packet);
    String n = packetStr.substring(0, 3);
    String r = packetStr.substring(3, 6);
    String g = packetStr.substring(6, 9);
    String b = packetStr.substring(9, 12);

    leds[n.toInt()].setRGB(r.toInt(), g.toInt(), b.toInt());
    FastLED.show();
      //Serial.println(n);
      //Serial.println(r);
      //Serial.println(g);
      //Serial.println(b);

      // Send return packet (needed to prevent overtransmitting and thus missed packets)
    UDP.beginPacket(UDP.remoteIP(), UDP.remotePort());
    UDP.write(reply);
    UDP.endPacket();
    }
  }

