/*
SSWebInteract.cs - Part of Simple Spectrum V2.1 by Sam Boyer.
*/

#if UNITY_WEBGL

using UnityEngine;
using System.Runtime.InteropServices;

public static class SSWebInteract
{
    [DllImport("__Internal")]
    private static extern void SimpleSpectrumGetData(byte[] array);

    [DllImport("__Internal")]
    private static extern void SimpleSpectrumGetLoudness(byte[] array);

    [DllImport("__Internal")]
    private static extern void SimpleSpectrumSetFFTSize(int fftSize);

    static byte[] freqBytesThisFrame; //since we can only use one Analyser, we might as well cache the result from the jslib.
    static float freqTime = -1; //remembers the last time we requested spectrum data.

    static byte[] loudnessBytes; //just a container so we avoid GC
    static float loudnessThisFrame; //no point calculating RMS multiple times a frame!    
    static float loudnessTime = -1; //remembers the last time we requested loudness data.

    //Gets raw spectrum data from the Web Audio API. This data is quite different from FMOD spectrum data, so you're probably best off using the methods in SimpleSpectrum.
    public static void GetSpectrumData(float[] samples) 
    {
        if(freqTime != Time.realtimeSinceStartup) //if the spectrum hasn't yet been made for this frame,
        {
            SimpleSpectrumGetData(freqBytesThisFrame); //call the jslib function

            freqTime = Time.realtimeSinceStartup;
        }

        int len = samples.Length;

        for (int i = 0; i < len; i++) //populate the samples array
        {
            samples[i] = (freqBytesThisFrame[i] / 512f);
            samples[i] *= samples[i];
        }
    }

    static int globalNumSamples = -1;

    public static int SetFFTSize(int numSamples) //sets the FFT size, and afterwards returns the currently set FFT size (since we're only allowed one).
    {
        if(globalNumSamples == -1) //if it's the first call,
        {
            SimpleSpectrumSetFFTSize(numSamples); //make this the fft size
            globalNumSamples = numSamples;
            freqBytesThisFrame = new byte[numSamples];
            loudnessBytes = new byte[numSamples * 2];
        }
        return globalNumSamples;
    }

    //Gets loudness from the Web Audio API. You're okay to use this method, but for portability you're probably better using OutputVolume.GetRMS that directly calls this.
    public static float GetLoudness()
    {
        if (loudnessTime != Time.realtimeSinceStartup) //if loudness hasn't yet been calculated for this frame,
        {
            SimpleSpectrumGetLoudness(loudnessBytes);  //call the jslib function

            int len = loudnessBytes.Length;
            int tot = 0;
            for (int i = 0; i < len; i++)
            {
                tot += Mathf.Abs(loudnessBytes[i] - 128); //soo technically this isn't RMS... but we'll see how it goes
            }
            loudnessThisFrame = tot / (float)(len * 128); //take mean, and divide by 128 cause the byte loudness is in range 0-128

            loudnessTime = Time.realtimeSinceStartup;
        }
        return loudnessThisFrame;
    }
}

#endif