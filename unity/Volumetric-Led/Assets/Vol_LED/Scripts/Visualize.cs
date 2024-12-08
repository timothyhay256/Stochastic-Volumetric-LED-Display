using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioSpectrumVisualization : MonoBehaviour
{
    AudioSource m_MyAudioSource;
    public int numBands = 5;  // Number of frequency bands
    public float smoothingFactor = 0.1f;  // Smoothing factor for peak tracking (adjust as needed)
    public float peakDecayRate = 0.95f;  // Rate of decay for peak tracking (adjust as needed)
    public float minDynamicRange = 0.1f;  // Minimum dynamic range to prevent division by zero

    private float[] spectrum;  // Array to store spectrum data
    private float[] bandAmplitudes;  // Array to store averaged amplitudes for each band
    private float[] smoothedAmplitudes;  // Array to store smoothed amplitudes for each band
    private float[] peakLevels;  // Array to store peak levels for each band
    private float[] dynamicRange;  // Array to store dynamic range for each band
    private int spectrumLength;  // Length of the spectrum array

    void Start()
    {
        m_MyAudioSource = GetComponent<AudioSource>();
        m_MyAudioSource.Play();

        spectrumLength = 1024;  // Set spectrum length (adjust as needed)
        spectrum = new float[spectrumLength];
        bandAmplitudes = new float[numBands];
        smoothedAmplitudes = new float[numBands];
        peakLevels = new float[numBands];
        dynamicRange = new float[numBands];

        // Initialize peak levels and dynamic range
        for (int i = 0; i < numBands; i++)
        {
            peakLevels[i] = 0f;
            dynamicRange[i] = minDynamicRange;
        }
    }
    
    void Update()
    {
        // Get spectrum data
        m_MyAudioSource.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);

        // Calculate samples per band
        int samplesPerBand = Mathf.Max(1, Mathf.FloorToInt((float)spectrumLength / (float)numBands));

        // Aggregate amplitude values for each band
        for (int band = 0; band < numBands; band++)
        {
            float sum = 0f;
            int startSample = band * samplesPerBand;
            int endSample = startSample + samplesPerBand;

            // Sum up amplitudes for this band
            for (int i = startSample; i < endSample; i++)
            {
                sum += spectrum[i];
            }

            // Average amplitude for this band
            bandAmplitudes[band] = sum / samplesPerBand;

            // Smooth the amplitude using exponential moving average
            smoothedAmplitudes[band] = Mathf.Lerp(smoothedAmplitudes[band], bandAmplitudes[band], smoothingFactor);

            // Update peak level for this band
            if (smoothedAmplitudes[band] > peakLevels[band])
            {
                peakLevels[band] = smoothedAmplitudes[band];
            }
            else
            {
                peakLevels[band] *= peakDecayRate;  // Decay peak level gradually
            }

            // Calculate dynamic range for this band
            dynamicRange[band] = Mathf.Max(peakLevels[band] - 0.001f, minDynamicRange);  // Ensure minimum range to avoid division by zero
        }

        // Visualize band amplitudes based on dynamic range
        for (int band = 0; band < numBands; band++)
        {
            // Calculate normalized amplitude within dynamic range
            float normalizedAmplitude = Mathf.Clamp(smoothedAmplitudes[band] / dynamicRange[band], 0f, 1f);

            // Apply logarithmic scale for better visualization
            float logScale = Mathf.Log(normalizedAmplitude + 1f) * 10f; // Adjust scale as needed

            // Map to visual range, considering logarithmic scale
            float height = Mathf.Lerp(0f, 100f, logScale);

            // Draw visualization (you can adjust the position and color as needed)
            Debug.DrawLine(new Vector3(band, 0, 0), new Vector3(band, height, 0), Color.Lerp(Color.red, Color.blue, (float)band / numBands));
        }
    }
}
