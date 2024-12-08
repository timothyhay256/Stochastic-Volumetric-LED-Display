using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingBlockDemo : MonoBehaviour
{
    public bool enableGravity = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (enableGravity) {
            enableGravity = false;
            foreach (Transform child in transform) {
                child.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            }
        }
    }
}
