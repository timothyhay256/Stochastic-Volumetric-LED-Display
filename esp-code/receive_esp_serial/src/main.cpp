#include <FastLED.h> // This just accepts commands via serial, and should work fine on basically any microprocessor.

#define LED_PIN 14
#define LED_COUNT 150
#define COLOR_ORDER GRB // Assuming the LED strip color order is GRB
#define BAUD_RATE 921600

int cycle = 0;
int set_every = 5;     // run show() every n assignments
bool sendBack = false; // Should I send back what instructions I just carried out? For debugging.

CRGB leds[LED_COUNT];

int n1, n2, n, r, g, b;
byte ack;

void setup()
{
    Serial.begin(BAUD_RATE);                                        // Set baud rate
    FastLED.addLeds<WS2811, LED_PIN, COLOR_ORDER>(leds, LED_COUNT); // Define LED strip
    FastLED.setBrightness(255);                                     // Set initial brightness
    FastLED.clear();                                                // Clear the LED strip
    FastLED.show();                                                 // Update LED strip
}

void loop()
{
    if (Serial.available() >= 6)
    { // Wait for start of packet bytes to be available
        if (Serial.read() == 0xFF)
        {
            if (Serial.read() == 0xBB)
            { // SOP bytes confirmed
                n1 = Serial.read();
                n2 = Serial.read(); // n1+n2 = uint16_t instead of uint8_t
                r = Serial.read();
                g = Serial.read();
                b = Serial.read();

                n = (n2 << 8) | n1; // Convert n1 and n2 to a uint16_t

                // Set the color of the specified LED
                leds[n] = CRGB(r, g, b);
                FastLED.show();

                if (sendBack)
                {
                    String message = String(n) + "|" + String(r) + "|" + String(g) + "|" + String(b);

                    // Print the message via Serial
                    Serial.println(message);
                }
                else
                {
                    Serial.write(0x01); // Send a single byte (acknowledgment)
                }
            }
        }
    }
}
