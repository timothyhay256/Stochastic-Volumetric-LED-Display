// Simple script to use ESP32 over UDP

#include <FastLED.h>
#include <WiFi.h>
#include <WiFiUdp.h>

// Change variables below!
#define LED_PIN 27
#define LED_COUNT 50
#define COLOR_ORDER GRB
const char *ssid = "SSID";         // Network SSID
const char *password = "PASSWORD"; // Network password
// Change variables above!

int port = 8888;

WiFiUDP UDP;
char packet[12];
char reply[] = "A";
char reply_bad[] = "BAD";

int cycle = 0;
int set_every = 0; // run show() every n assignments

CRGB leds[LED_COUNT];

void setup()
{
  Serial.begin(921600);
  Serial.println("Serial Begin");

  // Change below if you don't want to use parallel strands! See top of file for more info.
  FastLED.addLeds<WS2811, LED_PIN, COLOR_ORDER>(leds, LED_COUNT);
  FastLED.setBrightness(255);
  WiFi.begin(ssid, password);

  Serial.print("Connecting to ");
  Serial.print(ssid);

  while (WiFi.status() != WL_CONNECTED)
  {
    delay(500);
    Serial.print(".");
    leds[1] = CRGB(255, 0, 0);
    FastLED.show();
    delay(500);
    leds[1] = CRGB(0, 255, 0);
    FastLED.show();
  }
  leds[1] = CRGB(0, 255, 0);
  FastLED.show();
  Serial.println(" \nConnected!");
  Serial.print("IP: ");
  Serial.println(WiFi.localIP());
  Serial.print("Port: ");
  Serial.println(port);
  UDP.begin(port);
  Serial.println("Listening for packets...");
}

void loop()
{

  // If packet received...
  int packetSize = UDP.parsePacket();
  if (packetSize)
  {
    // Serial.print("Received packet! Size: ");
    // Serial.println(packetSize);
    int len = UDP.read(packet, 12);

    if (len > 0)
    {
      packet[len] = '\0';
    }
    // Serial.print("Packet received: ");
    // Serial.println(packet);
    String packetStr(packet);
    String n = packetStr.substring(0, 3);
    String r = packetStr.substring(3, 6);
    String g = packetStr.substring(6, 9);
    String b = packetStr.substring(9, 12);

    leds[n.toInt()] = CRGB(r.toInt(), g.toInt(), b.toInt());
    if (cycle >= set_every)
    {
      FastLED.show();
      cycle = 0;
    }
    else
    {
      cycle += 1;
    }
    Serial.println(n);
    Serial.println(r);
    Serial.println(g);
    Serial.println(b);

    // Send return packet (needed to prevent overtransmitting and thus missed packets)
    UDP.beginPacket(UDP.remoteIP(), UDP.remotePort());
    UDP.printf(reply);
    UDP.endPacket();
  }
}
