using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
public class colorChange : MonoBehaviour // TODO: Color selector for whole jar
{
    // public Material material;
    public Color32 color;
    public bool overrideColor;
    public int ledsToSet = 50;
    Thread mThread;
    public string connectionIP = "127.0.0.1";
    public int connectionPort = 5002;
    // public int index;
    // public GameObject led;
    // private GameObject newLed; // Modify attributes right after spawn, stores newest LED
    IPAddress localAdd;
    private bool collision = false;
    private bool exit;
    private float activeR;
    private float activeG;
    private float activeB;
    private byte[] myWriteBuffer;
    // TcpListener listener;
    UdpClient client;
    IPEndPoint ep;

    bool running;

    public ParticleSystem particleSystemCollision;
    // private ParticleCollisionEvent[] collisionEvents = new ParticleCollisionEvent[16];

    private void Update()
    {
        if (overrideColor) {
            // Debug.Log(color.r);
            activeR = color.r;
            activeG = color.g;
            activeB = color.b;
        }
    }

    private void Start()
    {
        ThreadStart ts = new ThreadStart(GetInfo);
        mThread = new Thread(ts);
        mThread.Start();
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
        if (overrideColor) {
            for (int index = 0; index <= ledsToSet; index++) {
                myWriteBuffer = Encoding.ASCII.GetBytes(index+"|"+activeR+"|"+activeG+"|"+activeB); //TODO: Get collision color
                client.Send(myWriteBuffer, myWriteBuffer.Length, ep); //Sending the data in Bytes to Python
                collision = false;
            }
        }
    }

}
