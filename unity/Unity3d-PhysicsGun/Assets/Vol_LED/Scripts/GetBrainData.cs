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

public class GetBrainData : MonoBehaviour
{
    public int attention;
    public int meditation;
    public int threshold;
    public int punishAttention;
    public int punishMeditation;

    Thread mThread;
    public string connectionIP = "127.0.0.1";
    public int connectionPort = 5003;
    IPAddress localAdd;
    TcpListener listener;
    TcpClient client;
    private bool running;
    // Start is called before the first frame update
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
            Debug.Log(dataReceived);
            string[] data = dataReceived.Split(',');

            attention = int.Parse(data[0]);
            meditation = int.Parse(data[1]);
            threshold = int.Parse(data[2]);
            punishAttention = int.Parse(data[3]);
            punishMeditation = int.Parse(data[4]);

            byte[] myWriteBuffer = Encoding.ASCII.GetBytes("ack"); //Converting string to byte data
            nwStream.Write(myWriteBuffer, 0, myWriteBuffer.Length); //Sending the data in Bytes to Python
        }
    }

    void OnDisable() {
        running = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
