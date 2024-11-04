/*
Reference
Implementing a Basic TCP Server in Unity: A Step-by-Step Guide
By RabeeQiblawi Nov 20, 2023
https://medium.com/@rabeeqiblawi/implementing-a-basic-tcp-server-in-unity-a-step-by-step-guide-449d8504d1c5
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenCover.Framework.Model;
using UnityEngine;
using Newtonsoft.Json;

public class TCP : MonoBehaviour
{
    const string hostIP = "127.0.0.1"; // Select your IP
    const int port = 80; // Select your port
    TcpListener server = null;
    TcpClient client = null;
    NetworkStream stream = null;
    Thread thread;

    public Transform LHand;
    public Transform RHand;
    public Transform Head;

    // Define your own message
    [Serializable]
    public class Message
    {
        public float LHand_x;
        public float LHand_y;
        public float LHand_z;
        public float RHand_x;
        public float RHand_y;
        public float RHand_z;
        public float Head_x;
        public float Head_y;
        public float Head_z;
    }

    private float timer = 0;
    private static object Lock = new object();
    private List<Message> MessageQue = new List<Message>();
    
    Matrix4x4 HomographyMatrix = Matrix4x4.identity;
    Vector3 PostShift = Vector3.zero;
    
    private void Start()
    {
        string matrixFile = System.IO.File.ReadAllText("Assets/LAB/CalibrationMatrix.json");
        float[,] floatArray = JsonConvert.DeserializeObject<float[,]>(matrixFile);
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                HomographyMatrix[i, j] = floatArray[i, j];
        HomographyMatrix = HomographyMatrix.transpose;
        string offsetFile = System.IO.File.ReadAllText("Assets/LAB/CalibrationPostShift.json");
        PostShift = JsonUtility.FromJson<Vector3>(offsetFile);
        
        thread = new Thread(new ThreadStart(SetupServer));
        thread.Start();
    }

    private void Update()
    {
        lock(Lock)
        {
            foreach (Message msg in MessageQue)
            {
                Move(msg);
            }
            MessageQue.Clear();
        }
    }

    private void SetupServer()
    {
        try
        {
            IPAddress localAddr = IPAddress.Parse(hostIP);
            server = new TcpListener(localAddr, port);
            server.Start();

            byte[] buffer = new byte[1024];
            string data = null;

            while (true)
            {
                Debug.Log("Waiting for connection...");
                client = server.AcceptTcpClient();
                Debug.Log("Connected!");

                data = null;
                stream = client.GetStream();

                // Receive message from client    
                int i;
                while ((i = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    data = Encoding.UTF8.GetString(buffer, 0, i);
                    Message message = Decode(data);
                    Debug.Log(message.ToString());
                    lock(Lock)
                    {
                        MessageQue.Add(message);
                    }
                }
                client.Close();
            }
        }
        catch (SocketException e)
        {
            Debug.Log("SocketException: " + e);
        }
        finally
        {
            server.Stop();
        }
    }

    private void OnApplicationQuit()
    {
        stream.Close();
        client.Close();
        server.Stop();
        thread.Abort();
    }

    public void SendMessageToClient(Message message)
    {
        byte[] msg = Encoding.UTF8.GetBytes(Encode(message));
        stream.Write(msg, 0, msg.Length);
        Debug.Log("Sent: " + message);
    }

    // Encode message from struct to Json String
    public string Encode(Message message)
    {
        return JsonUtility.ToJson(message, true);
    }

    // Decode messaage from Json String to struct
    public Message Decode(string json_string)
    {
        Message msg = JsonUtility.FromJson<Message>(json_string);
        return msg;
    }

    public void Move(Message message)
    {
        Vector3 LHandLocalPosition = new Vector3(message.LHand_x, message.LHand_y, message.LHand_z);
        Vector3 RHandLocalPosition = new Vector3(message.RHand_x, message.RHand_y, message.RHand_z);
        Vector3 HeadLocalPosition = new Vector3(message.Head_x, message.Head_y, message.Head_z);
        
        // LHandLocalPosition = HomographyMatrix.MultiplyPoint(LHandLocalPosition) + PostShift;
        // RHandLocalPosition = HomographyMatrix.MultiplyPoint(RHandLocalPosition) + PostShift;
        // HeadLocalPosition = HomographyMatrix.MultiplyPoint(HeadLocalPosition) + PostShift;
        
        LHand.localPosition = LHandLocalPosition;
        RHand.localPosition = RHandLocalPosition;
        Head.localPosition = HeadLocalPosition;
        
        Debug.Log("Left Hand: " + LHand.position.ToString());
        Debug.Log("Right Hand: " + RHand.position.ToString());
        Debug.Log("Head: " + Head.position.ToString());
    }
}
