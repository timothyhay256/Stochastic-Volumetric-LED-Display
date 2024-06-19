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

public class CSharpForGIT : MonoBehaviour
{
    Thread mThread;
    public string connectionIP = "127.0.0.1";
    public int connectionPort = 25001;
    public GameObject led;
    public GameObject ledHolder;
    private GameObject newLed; // Modify attributes right after spawn, stores newest LED
    IPAddress localAdd;
    private bool newPos = false;
    private int index = 0; // Python script always goes from 0 to numLeds, so index can be derived locally
    TcpListener listener;
    TcpClient client;
    Vector3 receivedPos = Vector3.zero;

    bool running;

    private void Update()
    {
        // transform.position = receivedPos; //assigning receivedPos in SendAndReceiveData()
        if (newPos) {
            newLed = Instantiate(led, receivedPos, Quaternion.identity);
            newLed.gameObject.GetComponent<SendCollision>().index = index;
            newLed.transform.SetParent(ledHolder.transform);
            index += 1;
            Debug.Log("Instantiate new object!");
            newPos = false;
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
        listener = new TcpListener(IPAddress.Any, connectionPort);
        listener.Start();

        client = listener.AcceptTcpClient();

        running = true;
        while (running)
        {
            SendAndReceiveData();
        }
        listener.Stop();
    }

    void SendAndReceiveData()
    {
        NetworkStream nwStream = client.GetStream();
        byte[] buffer = new byte[client.ReceiveBufferSize];

        //---receiving Data from the Host----
        int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize); //Getting data in Bytes from Python
        string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead); //Converting byte data to string

        if (dataReceived != null)
        {
            receivedPos = StringToVector3(dataReceived); //<-- assigning receivedPos value from Python
            newPos = true;
            // print("received pos data, and moved the Cube!");
            Debug.Log("Got pos data, set new prefab position.");

            //---Sending Data to Host----
            while (newPos) {
                ;
            }
            byte[] myWriteBuffer = Encoding.ASCII.GetBytes("ack"); //Converting string to byte data
            nwStream.Write(myWriteBuffer, 0, myWriteBuffer.Length); //Sending the data in Bytes to Python
        }
    }

    public static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }
    /*
    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }
    */
}