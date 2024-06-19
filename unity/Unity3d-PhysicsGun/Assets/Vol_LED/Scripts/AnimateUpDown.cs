using System.Collections; // BIG FAT TODO: handle inverted modes
using System.Collections.Generic;
// using System.Numerics;
using UnityEngine;

public class AnimateUpDown : MonoBehaviour
{
    public GameObject brainData;
    public GameObject animateWith;
    public GameObject ceiling;
    public float heightLim;
    public bool animateMeditation;
    public bool animateAttention;
    public float animTime;
    public bool pause = false; // Pause any targetPosition assignments
    private int attention;
    private int meditation;
    private int punishMeditation;
    private int punishAttention;
    private int threshold;
    private float multiplier;

    public Vector3 startPosition;
    private Vector3 ceilingStartPosition;
    public Vector3 targetPosition; // New target position for smooth animation
    private Vector3 velocity = Vector3.zero; // Velocity for smooth damping

    void Start()
    {
        startPosition = animateWith.transform.position;
        ceilingStartPosition = ceiling.transform.position;
        targetPosition = startPosition;
    }

    void Update()
    {
        attention = brainData.GetComponent<GetBrainData>().attention;
        meditation = brainData.GetComponent<GetBrainData>().meditation;
        punishAttention = brainData.GetComponent<GetBrainData>().punishAttention; // Mode
        punishMeditation = brainData.GetComponent<GetBrainData>().punishMeditation; // Mode
        threshold = brainData.GetComponent<GetBrainData>().threshold;
    
        if (animateAttention)
        {
            multiplier = heightLim / threshold;
            if (punishAttention == 1 || punishAttention == 0) {
                if (!pause) {
                    targetPosition = SetY(startPosition, startPosition.y + multiplier * attention);
                }
            } else if (punishAttention == 2) {
                // Debug.Log("Detecting going under threshold!");
                if (!pause) {
                    targetPosition = SetY(startPosition, startPosition.y + ((multiplier * attention) - heightLim) * -1); // If we want to detect going below threshold, apply different logic
                }
            }
        }
        else if (animateMeditation)
        {
            multiplier = heightLim / threshold;
            if (punishMeditation == 1 || punishMeditation == 0) {
                if (!pause) {
                    targetPosition = SetY(startPosition, startPosition.y + multiplier * meditation);
                }
            } else if (punishMeditation == 2) {
                if (!pause) {
                    targetPosition = SetY(startPosition, startPosition.y + ((multiplier * meditation) - heightLim) * -1);
                }  
            }
        }
        ceiling.transform.position = SetY(ceiling.transform.position, ceilingStartPosition.y + multiplier*threshold);

        // Smoothly move the object towards the target position
        animateWith.transform.position = Vector3.SmoothDamp(animateWith.transform.position, targetPosition, ref velocity, animTime);
    }

    Vector3 SetY(Vector3 vector, float y)
    {
        vector.y = y;
        return vector;
    }
}

/*
hl = 25
t = 30
att = 40
att_mode = 2

m = .83
att*m = 33.2
33.2 - hl = 7.8
Height is 7.8?
*/