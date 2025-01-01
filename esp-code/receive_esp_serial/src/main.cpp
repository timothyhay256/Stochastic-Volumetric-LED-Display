#include <FastLED.h> // This just accepts commands via serial, and should work fine on basically any microprocessor.


#define LED_PIN    14
#define LED_COUNT 150
#define COLOR_ORDER GRB // Assuming the LED strip color order is GRB
#define BAUD_RATE 921600

int cycle = 0; 
int set_every = 5; // run show() every n assignments
bool sendBack = false; // Should I send back what instructions I just carried out? For debugging.

CRGB leds[LED_COUNT];

int n, r, g, b;
byte ack;

void setup() {
  Serial.begin(BAUD_RATE); // Set baud rate
  FastLED.addLeds<WS2811, LED_PIN, COLOR_ORDER>(leds, LED_COUNT); // Define LED strip
  FastLED.setBrightness(255); // Set initial brightness
  FastLED.clear(); // Clear the LED strip
  FastLED.show(); // Update LED strip
}

void loop() {
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

                if (sendBack) {
                    String message = String(n) + "|" + String(r) + "|" + String(g) + "|" + String(b);
        
                    // Print the message via Serial
                    Serial.println(message);
                } else {
                    Serial.write(0x01); // Send a single byte (acknowledgment)
                }
            }
        }
    }
}
