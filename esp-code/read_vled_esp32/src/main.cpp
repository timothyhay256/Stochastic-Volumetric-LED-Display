/*
This script will play pre-programmed animations depending on what animation is selected. It also supports changing the animation playing using the ESP32s touch pins.
Note that it depends on the ESP32 due to being multicore, but if you want to run this on something else, just remove all the multicore sections and run `activeAnimationProcess` in a loop.

Place animations you wish to play inside of either animationZero or animationOne. If you desire, adding additional animations is easy, make an github issue if you want.
*/
// Touch0 is T0 which is on GPIO 4.

#include <FastLED.h>

// Add your animations here!
const uint8_t animationZero[] = {0x01, 0x02, 0x03, 0x04, 0xff, 0x01, 0xff, 0x00, 0x00, 0x02, 0x00, 0xff, 0x00, 0x03, 0x00, 0x00, 0xff}; // # This will wait for 255 ms, set LED 1 to red, 2, to green, and 3 to blue. Use this as a test animation.
int sizeOfAnimationZero = sizeof(animationZero) / sizeof(animationZero[0]);

const uint8_t animationOne[] = {0x01, 0x02, 0x03, 0x04, 0xff, 0x01, 0xff, 0x00, 0x00, 0x02, 0x00, 0xff, 0x00, 0x03, 0x00, 0x00, 0xff};
int sizeOfAnimationOne = sizeof(animationOne) / sizeof(animationOne[0]);

const uint8_t *activeAnimationArray;

// Change variables below!
#define TOUCH_PINS      // Uncomment this if you want to be able to switch the animation with a touch pin. Doing so will require a multicore microcontroller.
#define TOUCH_THRESH 20 // Increase this to increase the touch pin sensitivity, decrease to decrease the sensitivity.

#define BAUD_RATE 115200
#define LED_COUNT 50
#define LED_PIN 27
// Change variables above!

TaskHandle_t animationTask; // Task for multicore

int activeAnimation = 0;
int currentLoop = 0;
int activeR = 0;
int activeG = 0;
int activeB = 0;
int activeTime = 0;
int activeIndex = 0;

int test = 0;

int activeByteIndex = 0; // Track which byte we are looking at (index, r, g, b, timing)
int currentIndex = 0;    // Track how far into the array we are

CRGB leds[LED_COUNT];

void activeAnimationProcess(void *parameter);

void setup()
{
    Serial.begin(BAUD_RATE);

    FastLED.addLeds<WS2811, LED_PIN, RGB>(leds, LED_COUNT);
    FastLED.setBrightness(255);
    FastLED.clear();
    FastLED.show();

    xTaskCreatePinnedToCore(
        activeAnimationProcess, /* Function to implement the task */
        "animationTask",        /* Name of the task */
        10000,                  /* Stack size in words */
        NULL,                   /* Task input parameter */
        0,                      /* Priority of the task */
        &animationTask,         /* Task handle. */
        0);                     /* Core where the task should run */
}

void loop()
{
#ifdef TOUCH_PINS
    if (touchRead(4) <= TOUCH_THRESH)
    {
        while (touchRead(4) <= 20)
        {
            delay(10);
        }
        if (activeAnimation == 0)
        {
            Serial.println("Animation 1");
            activeAnimation = 1;
            FastLED.clear();
        }
        else if (activeAnimation == 1)
        {
            activeAnimation = 0;
            FastLED.clear();
            Serial.println("Animation 0");
        }
    }
#endif

#if !defined(TOUCH_PINS)
    activeAnimationProcess(NULL);
#endif
}

void activeAnimationProcess(void *parameter)
{
    for (;;)
    {
        // delay(1000);
        switch (activeAnimation)
        {
        case 0:
            activeAnimationArray = animationZero;
            if (currentIndex >= sizeOfAnimationZero)
            {
                currentIndex = 0;
                Serial.println("Resetting index");
            }
            break;
        case 1:
            activeAnimationArray = animationOne;
            if (currentIndex >= sizeOfAnimationOne)
            {
                currentIndex = 0;
            }
            break;
        default:
            break;
        }

        if (activeAnimationArray[currentIndex] == 1 && activeAnimationArray[currentIndex + 1] == 2 && activeAnimationArray[currentIndex + 2] == 3 && activeAnimationArray[currentIndex + 3] == 4)
        {
            delay(activeAnimationArray[currentIndex + 4]);
            currentIndex += 5;
        }
        else
        { // No timing instruction, assume it is
            activeIndex = activeAnimationArray[currentIndex];
            activeR = activeAnimationArray[currentIndex + 1];
            activeG = activeAnimationArray[currentIndex + 2];
            activeB = activeAnimationArray[currentIndex + 3];

            leds[activeIndex] = CRGB(activeR, activeG, activeB);
            FastLED.show();

            currentIndex += 4;
        }
    }
}
