using System.Collections; 
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;

public class SendCollision : MonoBehaviour
{
    Thread mThread;
    public string connectionIP = "127.0.0.1";
    public int connectionPort = 5002;
    public int index;
    // public GameObject led;
    // private GameObject newLed; // Modify attributes right after spawn, stores newest LED
    IPAddress localAdd;
    private bool collision = false;
    private bool exit;
    private int activeR;
    private int activeG;
    private int activeB;
    private int collCount;
    private Color32 firstColor;
    private Color32 middleColor;
    // TcpListener listener;
    UdpClient client;
    IPEndPoint ep;

    bool running;

    public ParticleSystem particleSystemCollision;
    // private ParticleCollisionEvent[] collisionEvents = new ParticleCollisionEvent[16];

    private void Update()
    {

    }

    void Start()
    {
        ThreadStart ts = new ThreadStart(GetInfo);
        mThread = new Thread(ts);
        mThread.Start();
        var rend = GetComponent<Renderer>();
    }

    void GetInfo()
    {
        localAdd = IPAddress.Parse(connectionIP);
        // client = new UdpClient();
        client = new UdpClient();
        // client.Connect(localAdd, connectionPort);
        ep = new IPEndPoint(localAdd, connectionPort);
        // client.Connect(ep);

        running = true;
        while (running) 
        {
            Thread.Sleep(50); // Prevent extreme CPU usage (idk why)
            SendAndReceiveData();
        }
    }

    void SendAndReceiveData()
    {
        if (collision) {
            // Debug.Log("Sending collision for index "+index);
            byte[] myWriteBuffer = Encoding.ASCII.GetBytes(index+"|"+activeR+"|"+activeG+"|"+activeB); 
            client.Send(myWriteBuffer, myWriteBuffer.Length, ep); //Sending the data in Bytes to Python
            collision = false;
        } else if (exit) {
            // Debug.Log("Sending exit for index "+index);
            byte[] myWriteBuffer = Encoding.ASCII.GetBytes(index+"E"); //Converting string to byte data
            client.Send(myWriteBuffer, myWriteBuffer.Length, ep); //Sending the data in Bytes to Python
            exit = false;
        }
    }

    private void OnTriggerEnter(Collider col) {
        if (col.tag != "LED") {
            collCount += 1;
            if (collCount == 1) {
                firstColor = col.GetComponent<MeshRenderer>().material.color;
            }
            // } else if (collCount == 2) {
            //     middleColor = col.GetComponent<MeshRenderer>().material.color;
            // }
            Color32 objColor;
            objColor = col.GetComponent<MeshRenderer>().material.color;
            activeR = objColor.r;
            activeG = objColor.g;
            activeB = objColor.b;
            var rend = GetComponent<Renderer>();
            rend.material.SetColor("_Color", objColor);
            collision = true;
            }
    }

    private void OnTriggerExit(Collider col) {
        collCount -= 1;
        if (collCount == 1) {
            activeR = firstColor.r;
            activeG = firstColor.g;
            activeB = firstColor.b;
            var rend = GetComponent<Renderer>();
            rend.material.SetColor("_Color", firstColor);
            collision = true;
        }
        // } else if (collCount == 2) {
        //     activeR = middleColor.r;
        //     activeG = middleColor.g;
        //     activeB = middleColor.b;
        //     var rend = GetComponent<Renderer>();
        //     rend.material.SetColor("_Color", middleColor);
        //     collision = true;
        // }
        else {
            Color32 objColor = new Color32(255, 255, 255, 255);
            var rend = GetComponent<Renderer>();
            rend.material.SetColor("_Color", objColor);
            Debug.Log("Trigger exit!");
            exit = true;
        }
    }

    void OnDisable() {
        running = false;
    }

    void OnParticleCollision(GameObject other) { // TODO: Exit timeout for particles. There is no particle collision exit.
        // other.GameObject.Particle.GetCurrentColor;
        Debug.Log("Particle enter!");
        collision = true;
    }
}