using UnityEngine;

public class Example : MonoBehaviour
{
    // Get list of Microphone devices and print the names to the log
    void Start()
    {
        foreach (var device in Microphone.devices)
        {
            Debug.Log("Name: " + device);
        }
    }
}