#include <FastLED.h> // NOTE: Neopixel will NOT work when using serial! FastLED does however.

#define LED_PIN    14
#define LED_COUNT 150
#define COLOR_ORDER GRB // Assuming the LED strip color order is GRB
#define BAUD_RATE 921600

int cycle = 0; 
int set_every = 5; // run show() every n assignments

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
  if (Serial.available() >= 4) { // Check if there are at least 4 bytes available
    n = Serial.read(); // Read the first byte and assign it to n
    r = Serial.read(); // Read the second byte and assign it to r
    g = Serial.read(); // Read the third byte and assign it to g
    b = Serial.read(); // Read the fourth byte and assign it to b

    // Set the color of the specified LED
    leds[n] = CRGB(r, g, b);
    if (cycle >= set_every) {
        FastLED.show();
        cycle = 0;
    } else {
        cycle += 1;
    }

    // Send acknowledgment back to Python
    Serial.write(0x01); // Send a single byte (acknowledgment)
  }
}
