// Remotely control addressable LEDs over WiFi - UDP on ESP32 with FastLED. Single core >_<
#include <NeoPixelBus.h> // TODO: Convert to FastLED so it works. 
#include <WiFi.h>
#include <WiFiUdp.h>

#define LED_PIN    13
#define LED_COUNT 150
RgbColor red(255, 0, 0);
RgbColor green(0, 255, 0);
RgbColor clear(0);

int port = 8888;
const char *ssid = "SSID";
const char *password = "PASSWORD";

WiFiUDP UDP;
char packet[12];
// char reply[] = "A\r\n";
uint8_t reply[50] = "A\r\n";
char reply_bad[] = "BAD";

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
  // UDP.beginPacket(UDP.remoteIP(), UDP.remotePort());
}

void loop() {

  // If packet received...
  int packetSize = UDP.parsePacket();
  if (packetSize) {
    Serial.print("Received packet! Size: ");
    Serial.println(packetSize);
    int len = UDP.read(packet, 12);

    if (len > 0)
    {
      packet[len] = '\0';
    }
      Serial.print("Packet received: ");
      Serial.println(packet);
    String packetStr(packet);
    String n = packetStr.substring(0, 3);
    String r = packetStr.substring(3, 6);
    String g = packetStr.substring(6, 9);
    String b = packetStr.substring(9, 12);
    Serial.println("Setting color");
    RgbColor color(r.toInt(), g.toInt(), b.toInt());
    strip.SetPixelColor(n.toInt(), color);
    strip.Show();
      //Serial.println(n);
      //Serial.println(r);
      //Serial.println(g);
      //Serial.println(b);

      // Send return packet (needed to prevent overtransmitting and thus missed packets)
    Serial.println("Begin packet");
    Serial.println(UDP.remoteIP());
    Serial.println(UDP.remotePort());
    UDP.beginPacket(UDP.remoteIP(), UDP.remotePort());
    // delay(500);
    Serial.println("Writing response");
    UDP.printf("A\r\n");
    // UDP.write(reply, sizeof(reply));
    // Serial.println("End packet");
    UDP.endPacket();
    }
  }

