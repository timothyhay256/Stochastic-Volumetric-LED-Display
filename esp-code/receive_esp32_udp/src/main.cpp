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
  int packetSize = UDP.parsePacket();
  if (packetSize)
  {
    int len = UDP.read(packet, 5);

    if (len == 4)
    {
      byte n1 = packet[0];
      byte n2 = packet[1];
      byte r = packet[2];
      byte g = packet[3];
      byte b = packet[4];

      int n = (n2 << 8) | n1; // Convert n1 and n2 to a uint16_t

      leds[n] = CRGB(r, g, b);

      if (cycle >= set_every)
      {
        FastLED.show();
        cycle = 0;
      }
      else
      {
        cycle += 1;
      }

      UDP.beginPacket(UDP.remoteIP(), UDP.remotePort());
      UDP.printf(reply);
      UDP.endPacket();
    }
  }
}