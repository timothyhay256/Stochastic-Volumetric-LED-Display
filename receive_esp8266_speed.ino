// Remotely control Neopixel over WiFi - UDP and quicker buffers, and delayed strip.set for speed
#include <NeoPixelBus.h>
#include <ESP8266WiFi.h>
#include <WiFiUdp.h>

#define LED_PIN    3
#define LED_COUNT 150
RgbColor red(255, 0, 0);
RgbColor green(0, 255, 0);
RgbColor clear(0);

int port = 8888;
const char *ssid = "SSID";
const char *password = "PASSWORD";

WiFiUDP UDP;
char packet[12];
char reply[] = "A";
char reply_bad[] = "BAD";

int counter = 0; // Used to only strip.set a interval
int interval = 3; // How many writes to wait before strip.set

NeoPixelBus<NeoRgbFeature, Neo800KbpsMethod> strip(LED_COUNT, LED_PIN);

void setup() {
  Serial.begin(115200);
  Serial.println("Serial Begin");

  strip.Begin();  //This took way to long to figure out, this is not ok
  strip.Show();
  WiFi.begin(ssid, password);

  Serial.print("Connecting to ");
  Serial.print(ssid);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
    strip.SetPixelColor(0, red);
    strip.Show();
    delay(500);
    strip.SetPixelColor(0, clear);
    strip.Show();
  }
  strip.SetPixelColor(0, green);
  strip.Show();
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

    RgbColor color(r.toInt(), g.toInt(), b.toInt());
    strip.SetPixelColor(n.toInt(), color);
    if (counter == interval) {
        counter = 0;
        strip.Show();
    } else {
        counter ++;
    }
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

