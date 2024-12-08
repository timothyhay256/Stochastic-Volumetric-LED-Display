using UnityEngine;

public class MicrophonePlayback : MonoBehaviour
{
    AudioSource audioSource;
    string microphoneName = null; // Name of the microphone to use
    public int device = 0;

    void Start()
    {
        // Initialize AudioSource
        audioSource = GetComponent<AudioSource>();

        // Check available microphones
        if (Microphone.devices.Length > 0)
        {
            microphoneName = Microphone.devices[device]; // Select the first available microphone
            Debug.Log("Using audio source: "+microphoneName);
        }
        else
        {
            Debug.LogError("No microphone detected!");
            return;
        }

        // Start recording from the selected microphone into an AudioClip
        AudioClip microphoneClip = Microphone.Start(microphoneName, true, 10, AudioSettings.outputSampleRate);
        
        // Wait until recording has started
        while (!(Microphone.GetPosition(microphoneName) > 0)) { }

        // Assign the recorded AudioClip to the AudioSource
        audioSource.clip = microphoneClip;
    }

    void Update()
    {
        // Example: Play or pause the audio based on user input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
            else
            {
                audioSource.Pause();
            }
        }
    }
}
