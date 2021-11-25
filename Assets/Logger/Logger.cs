using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Collections.Generic;

public class Logger : ScriptableObject
{
    /* instance member */
    [SerializeField] private string serverIp = "10.112.58.138";
    [SerializeField] private int serverPort = 10004;

    public static Socket sock;
    private static Logger instance;
    private static SemaphoreSlim connectSemaphore = new SemaphoreSlim(1, 1);
    private static SemaphoreSlim sendSemaphore = new SemaphoreSlim(1, 1);
    private static Queue<byte[]> sendQueue = new Queue<byte[]>();
    private const int HEADER_SIZE = 10;
    private const int LOG_CONTENT_SIZE = 8 * 1024;
    private const bool LOG_ENABLE = false;


    public static Logger Instance
    {
        get
        {
            if (!instance)
            {
                instance = FindObjectOfType<Logger>();
            }
            if (!instance)
            {
                instance = Resources.FindObjectsOfTypeAll<Logger>().FirstOrDefault();
            }
            return instance;
        }
        set
        {
            instance = value;
        }
    }
    private async void OnEnable()
    {
        sock?.Close();
        sock?.Dispose();
        sock = null;
        sendQueue.Clear();
        await Connect();
        _ = System.Threading.Tasks.Task.Run(() =>
        {
            while (sock != null && sock.Connected)
            {
                if (sendQueue.Count != 0)
                {
                    Send(sendQueue.Dequeue());
                }
            }
            sock?.Close();
            sock?.Dispose();
            sock = null;
            sendQueue.Clear();
        });
    }

    void OnDisable()
    {
        Disconnect();
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    public static void Log(string logString, string stackTrace = "", LogType type = LogType.Log, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = null, [CallerLineNumber] int lineNo = -1)
    {
        Debug.Log(logString);
        if (!LOG_ENABLE) return;
        byte[] packet = new byte[HEADER_SIZE + LOG_CONTENT_SIZE];
        using (MemoryStream ms = new MemoryStream(packet))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            // 0: log 1: frame 2: close
            bw.Write("0");
        }

        byte[] cPacket = new byte[LOG_CONTENT_SIZE];

        using (MemoryStream ms = new MemoryStream(cPacket))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write($"{type.GetHashCode()}");
            bw.Write($"{DateTime.Now.ToString("hh:mm:ss")}");
            bw.Write($"{logString}");
            if (string.IsNullOrEmpty(stackTrace))
            {
                stackTrace += $"{methodName} (at {fileName}:{lineNo})";
            }
            bw.Write($"{stackTrace}");
            bw.Write("");
        }

        Buffer.BlockCopy(cPacket, 0, packet, HEADER_SIZE, LOG_CONTENT_SIZE);
        SendAsync(packet);
    }

    public static void LogHidden(string logString, string stackTrace = "", LogType type = LogType.Log, [CallerMemberName] string methodName = null, [CallerFilePath] string fileName = null, [CallerLineNumber] int lineNo = -1)
    {
        Debug.Log(logString);
        byte[] packet = new byte[HEADER_SIZE + LOG_CONTENT_SIZE];
        using (MemoryStream ms = new MemoryStream(packet))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            // 0: log 1: frame 2: close
            bw.Write("0");
        }

        byte[] cPacket = new byte[LOG_CONTENT_SIZE];

        using (MemoryStream ms = new MemoryStream(cPacket))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write($"{type.GetHashCode()}");
            bw.Write($"{DateTime.Now.ToString("hh:mm:ss")}");
            bw.Write($"{logString}");
            if (string.IsNullOrEmpty(stackTrace))
            {
                stackTrace += $"{methodName} (at {fileName}:{lineNo})";
            }
            bw.Write($"{stackTrace}");
            bw.Write("");
        }

        Buffer.BlockCopy(cPacket, 0, packet, HEADER_SIZE, LOG_CONTENT_SIZE);
        SendAsync(packet);
    }

    public static void Frame(byte[] frame)
    {
        byte[] packet = new byte[HEADER_SIZE + frame.Length];
        using (MemoryStream ms = new MemoryStream(packet))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            // 0: log 1: frame 2: close
            bw.Write("1");
            bw.Write($"{frame.Length}");
        }

        Buffer.BlockCopy(frame, 0, packet, HEADER_SIZE, frame.Length);
        SendAsync(packet);
    }

    private static void SendAsync(byte[] packet)
    {
        sendQueue.Enqueue(packet);
    }

    private static async void Send(byte[] packet)
    {
        if (sock == null)
        {
            await Connect();
        }
        //_ = System.Threading.Tasks.Task.Run(() =>
        //{
        //    sendSemaphore.Wait();
        var sended = 0;
        var length = packet.Length;
        while (sended < length)
        {
            sended += sock.Send(packet, sended, length - sended, SocketFlags.None);
            Debug.Log($"packet sended: {sended}, packet[0]:{packet[0]}, ThreadID:{Thread.CurrentThread.ManagedThreadId}");
            Thread.Sleep(5);
        }
        //    sendSemaphore.Release();
        //});
    }

    private static async System.Threading.Tasks.Task Connect()
    {
        if (!Instance)
        {
            Debug.Log("Not Found Logger instance");
            return;
        }
        try
        {
            //connectSemaphore.Wait();
            if (sock != null)
            {
                Debug.Log($"sock.connected: {sock.Connected}");

                //connectSemaphore.Release();
                return;
            }
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var iEP = new IPEndPoint(IPAddress.Parse(Instance.serverIp), Instance.serverPort);
            Debug.Log($"Try To Connect Echo Server {iEP}");
            sock.Connect(iEP);
            Application.quitting += Disconnect;
            //connectSemaphore.Release();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    public static void Disconnect()
    {
        try
        {
            byte[] hPakcet = new byte[10];

            using (MemoryStream ms = new MemoryStream(hPakcet))
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write("2");
                bw.Write("");
                sendQueue.Enqueue(hPakcet);
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }
}