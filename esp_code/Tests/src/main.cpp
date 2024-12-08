#include <FastLED.h>

// Define the number of LEDs
#define NUM_LEDS 98

// Define the pin where the data line is connected
#define DATA_PIN 27

// Create a FastLED object
CRGB leds[NUM_LEDS];

void setup() {
  // Initialize FastLED with the LED type and data pin
  FastLED.addLeds<WS2812B, DATA_PIN, GRB>(leds, NUM_LEDS);

  // Fill the LEDs with white color
  fill_solid(leds, NUM_LEDS, CRGB::White);

  // Show the LEDs with the new color
  FastLED.show();
}

void loop() {
  // Nothing to do here
  fill_solid(leds, NUM_LEDS, CRGB::White);

  // Show the LEDs with the new color
  FastLED.show();
}
