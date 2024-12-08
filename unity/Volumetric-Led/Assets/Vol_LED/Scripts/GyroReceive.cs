using UnityEngine; 
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

public class UDPReceiver : MonoBehaviour
{
    public int port = 5011;  // UDP port to listen on
    public bool centerOrigin = false;
    public float amplitudeDivisor = 5f;
    public int activeBar;
    private float lowestX, highestX, lowestZ, highestZ, midpointX, midpointY, midpointZ; 

    private float w, x, y, z;
    private bool newData = false;
    public bool clearLeds = false; // When set to true, percentFilled will always be 0
    private int clearLedFrameCounter = 0; 
    private int clearLedFrameCount = 25; // How many frames to keep percentFilled at 0
    private UdpClient udpClient;
    private Quaternion targetRot;
    public GameObject anime;
    public GameObject tempHolder;
    public GameObject musicVisualizer;

    private SimpleSpectrum simpleSpectrum;
    public float percentFilled = .5f; // 50%
    public int yawOffset; // Add YPR offsets 
    public int pitchOffset; 
    public int rollOffset;

    private Vector3 addRot;

    private float lowestY;
    private float highestY;
    private Transform highestYChild = null;
    private Transform lowestYChild = null;
    public float bar1, bar2, bar3, bar4, bar5;

    void Start()
    {
        // Create UDP client
        udpClient = new UdpClient(port);

        // Start listening for UDP messages
        udpClient.BeginReceive(ReceiveCallback, null);

        Debug.Log("UDP Receiver started on port " + port);
        // targetRot = new Quaternion(1, .64f, .23f, .43f);
        // transform.rotation = targetRot;
        // Serial.println(GetLowestPoint);
        simpleSpectrum = musicVisualizer.GetComponent<SimpleSpectrum>();
    }

    void ReceiveCallback(System.IAsyncResult ar)
    {
        // Get the received UDP datagram
        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);
        byte[] receivedBytes = udpClient.EndReceive(ar, ref ipEndPoint);
        string receivedMessage = Encoding.ASCII.GetString(receivedBytes).Trim();

        string[] quaternion = receivedMessage.Split(',');
        if (quaternion.Length >= 4)
        {
            w = float.Parse(quaternion[0]);
            x = float.Parse(quaternion[1]);
            y = float.Parse(quaternion[2]);
            z = float.Parse(quaternion[3]);

            // Debug log for verification
            Debug.Log("Received quaternion: " + w + ", " + x + ", " + y + ", " + z);
            // targetRot = new Quaternion(x, y, z, w);
            newData = true;
            // transform.rotation = targetRot;
        }
        // Continue listening for more UDP messages
        udpClient.BeginReceive(new System.AsyncCallback(ReceiveCallback), null);
    }

    void OnApplicationQuit()
    {
        // Clean up UDP client
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }

    private void Update() {
        bar1 = simpleSpectrum.bar1;
        bar2 = simpleSpectrum.bar2;
        bar3 = simpleSpectrum.bar3;
        bar4 = simpleSpectrum.bar4;
        bar5 = simpleSpectrum.bar5;

        // Debug.Log("Bars: "+bar1+" "+bar2+" "+bar3+" "+bar4+" "+bar5);
        if (newData) {
            // Serial.println("Setting Quaternion");
            targetRot = new Quaternion(-x, z, y, w);
            addRot = new Vector3(pitchOffset, yawOffset, rollOffset);
            Quaternion addRotQuat = Quaternion.Euler(addRot);
            targetRot = targetRot * addRotQuat;
            transform.rotation = targetRot;
            newData = !newData;
        }

        highestY = float.MinValue;
        lowestY = float.MaxValue;
        foreach (Transform child in transform)
        {
            // Check if the child has a specific tag (e.g., "Anime")
            if (child.CompareTag("Initial"))
            {
                // Skip this child if it has the "Anime" tag
                continue;
            }

            // Check the Y position of the child
            float childY = child.position.y;

            // Update if this child has a higher Y position
            if (childY > highestY)
            {
                highestY = childY;
            }
            if (childY < lowestY){
                lowestY = childY;
            }
        }

        // Get proper "percentage"
        if (clearLeds) {
            percentFilled = 0f;
            clearLedFrameCounter ++;
            if (clearLedFrameCounter >= clearLedFrameCount) {
                clearLeds = false;
            } 
        } else if (activeBar == 1) {
            percentFilled = bar1 / amplitudeDivisor;
        } else if (activeBar == 2) {
            percentFilled = bar2 / amplitudeDivisor;
        } else if (activeBar == 3) {
            percentFilled = bar3 / amplitudeDivisor;
        } else if (activeBar == 4) {
            percentFilled = bar4 / amplitudeDivisor;
        } else if (activeBar == 5) {
            percentFilled = bar5 / amplitudeDivisor;
        }

        // Debug.Log("PercentFilled is "+percentFilled);

        // Set Anime height to the proper "percentage"
        float newY = ((highestY - lowestY) * percentFilled) + lowestY;
        Vector3 newPosition = anime.transform.position;
        newPosition.y = newY;
        anime.transform.position = newPosition;

        // Debug.Log(highestY);
        // Debug.Log(lowestY);

        if (centerOrigin) {
            Debug.Log("Centering origin!");
            centerOrigin = false;
            highestX = float.MinValue;
            highestZ = float.MinValue;

            lowestX = float.MaxValue;
            lowestZ = float.MaxValue;

            foreach (Transform child in transform) { // Get limits of LEDs to calculate center
                float childX = child.transform.position.x;
                float childZ = child.transform.position.z;

                if (childX > highestX) {
                    highestX = childX;
                }
                if (childX < lowestX) {
                    lowestX = childX;
                }

                if (childZ > highestZ) {
                    highestZ = childZ;
                }
                if (childZ < lowestZ) {
                    lowestZ = childZ;
                }
            }
        midpointX = (lowestX + highestX) / 2; // Get midpoints
        midpointY = (lowestY + highestY) / 2;
        midpointZ = (lowestZ + highestZ) / 2;

        Debug.Log("fds");
        Debug.Log(midpointX);
        Debug.Log(midpointY);
        Debug.Log(midpointZ);

        List<Transform> childrenToMove = new List<Transform>();

        foreach (Transform child in transform) {
            childrenToMove.Add(child); // Add each child to the list
        }
        
        foreach (Transform child in childrenToMove) {
            child.SetParent(tempHolder.transform, true);
        }

        transform.position = new Vector3(midpointX, midpointY, midpointZ);

        foreach (Transform child in childrenToMove) {
            child.SetParent(transform, true);
        }
        }
    }
}
