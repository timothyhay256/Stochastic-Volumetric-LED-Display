#include <Arduino.h>
#include <FastLED.h> 
// Manually set pins inside of setup(), and set LED_COUNT_PER_STRIP and NUM_STRIPS. This FastLED setup section can also be used for any other scripts. 
// ESP8266: WS2811_PORTA - pins 12, 13, 14 and 15 (or pins 6,7,5 and 8 on the NodeMCU boards/pin layout).   From FastLED wiki
// ESP32: Manually set pins

#define LED_COUNT_PER_STRIP 50 
#define NUM_STRIPS 2
// #define COLOR_ORDER GRB // Assuming the LED strip color order is GRB
bool sendBack = false; // Should I send back what instructions I just carried out? For debugging.

#define BAUD_RATE 921600

CRGB leds[LED_COUNT_PER_STRIP * NUM_STRIPS];

int cycle = 0; 
int set_every = 0; // run show() every n assignments
int n, r, g, b;
byte ack;

void setup() {
  Serial.begin(BAUD_RATE);

  // For ESP8266 
  // FastLED.addLeds<WS2811_PORTA,NUM_STRIPS, RGB>(leds, LED_COUNT_PER_STRIP);
  
  // For ESP32
  FastLED.addLeds<WS2811, 27, GRB>(leds, LED_COUNT_PER_STRIP);
  FastLED.addLeds<WS2811, 13, GRB>(leds + LED_COUNT_PER_STRIP, LED_COUNT_PER_STRIP); 
  // FastLED.addLeds<WS2811, 13, RGB>(leds, LED_COUNT_PER_STRIP, LED_COUNT_PER_STRIP); 
  // FastLED.addLeds<WS2811, 14, RGB>(leds, 2 * LED_COUNT_PER_STRIP, LED_COUNT_PER_STRIP); 
  // FastLED.addLeds<WS2811, 26, RGB>(leds, 3 * LED_COUNT_PER_STRIP, LED_COUNT_PER_STRIP); 

  FastLED.setBrightness(255); 
  FastLED.clear(); 
  FastLED.show(); 
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