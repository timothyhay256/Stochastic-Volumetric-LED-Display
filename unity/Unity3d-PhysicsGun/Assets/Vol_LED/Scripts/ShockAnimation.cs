using System.Collections; // TODO: When shocking, set updown to ground and wait until anim over to prevent conflicts.
using System.Collections.Generic; // TODO: Signal strength: wait until reached good strength.
using UnityEngine;

public class ShockAnimation : MonoBehaviour
{
    public bool animateMeditation;
    public bool animateAttention;
    public float animTime;
    public float scaleMultiplier = 5f;
    public bool manualActivate;
    private int attention;
    private int meditation;
    private int punishMeditation;
    private int punishAttention;
    private int threshold;
    private bool blockAnim;
    private Vector3 origSize;
    private Vector3 targetSize;

    public GameObject brainData;
    public GameObject animateWith;
    public GameObject upDownAnim;
    public GameObject upDownAnimMeditation;
    private Vector3 velocity = Vector3.zero; // Velocity for smooth damping
    private Vector3 largeSize;
    private Collider upDownCollide;
    // Start is called before the first frame update
    void Start()
    {
        origSize = animateWith.transform.localScale;
        targetSize = origSize;
        largeSize = new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);
        // upDownCollide = upDownAnim.GetComponent<>().GetComponent<Collider>();'
        animateAttention = false;
        animateMeditation = false;
    }

    // Update is called once per frame
    void Update()
    {
        largeSize = new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);
        attention = brainData.GetComponent<GetBrainData>().attention;
        meditation = brainData.GetComponent<GetBrainData>().meditation;
        punishAttention = brainData.GetComponent<GetBrainData>().punishAttention; // Mode
        punishMeditation = brainData.GetComponent<GetBrainData>().punishMeditation; // Mode
        threshold = brainData.GetComponent<GetBrainData>().threshold;

        if (punishAttention == 1 || punishAttention == 2) {
            animateAttention = true;
            animateMeditation = false;
        }
        if (punishMeditation == 1 || punishMeditation == 2) {
            animateMeditation = true;
            animateAttention = false;
        }
        
        if (manualActivate) {
            manualActivate = false;

            animation();
        }
        if (animateAttention) {
            if (punishAttention == 1) {
                if (attention >= threshold) {
                    animation();
                }
            } else if (punishAttention == 2) {
                if (attention <= threshold) {
                    animation();
                }
            }
        }
        if (animateMeditation) {
            if (punishMeditation == 1) {
                if (meditation >= threshold) {
                    animation();
                }
            } else if (punishMeditation == 2) {
                if (meditation <= threshold) {
                    animation();
                }
            }
        }
        animateWith.transform.localScale = Vector3.SmoothDamp(animateWith.transform.localScale, targetSize, ref velocity, animTime);
    }

    void animation() {
        if (!blockAnim) {
            blockAnim = true;
            // upDownCollide.enabled = false;
            upDownAnim.GetComponent<AnimateUpDown>().pause = true;
            upDownAnim.GetComponent<AnimateUpDown>().targetPosition = upDownAnim.GetComponent<AnimateUpDown>().startPosition;

            upDownAnimMeditation.GetComponent<AnimateUpDown>().pause = true;
            upDownAnimMeditation.GetComponent<AnimateUpDown>().targetPosition = upDownAnimMeditation.GetComponent<AnimateUpDown>().startPosition;
            targetSize = largeSize;
            StartCoroutine(returnSize());
        }
    }

    IEnumerator returnSize() {
        Debug.Log("returnSize called!");
        yield return new WaitForSeconds(animTime*2);
        targetSize = origSize;
        blockAnim = false;
        yield return new WaitForSeconds(animTime+.25f); // Wait for cube to return to normal size before unpausing animation
        if (!blockAnim) {
            upDownAnim.GetComponent<AnimateUpDown>().pause = false;

            upDownAnimMeditation.GetComponent<AnimateUpDown>().pause = false;
        }
        // upDownCollide.enabled = true;
    }
}
