using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;

public class Logger : ScriptableObject
{
    [SerializeField] private string serverIp = "127.0.0.1";
    [SerializeField] private int serverPort = 0;

    private static Socket sock;
    private static Logger instance;

    private static byte[] packet = new byte[1024];
    private static MemoryStream ms;
    private static BinaryWriter bw;

    public static Logger Instance
    {
        get
        {
            if (!instance)
            {
                var loggers = FindObjectOfType<Logger>();
                instance = FindObjectOfType<Logger>();
            }
            if (!instance)
            {
                var loggers = Resources.FindObjectsOfTypeAll<Logger>();
                instance = Resources.FindObjectsOfTypeAll<Logger>().FirstOrDefault();
            }
            return instance;
        }
        set
        {
            instance = value;
        }
    }

    void OnDisable()
    {
        Disconnect();
    }

    public static void Log(string logString, string stackTrace = "", LogType type = LogType.Log)
    {
        Debug.Log("Logger.Log is called");
        if (sock == null)
        {
            InitStream();
            InitSocketAndConnect();
        }
        Debug.Log(logString);
        switch (type)
        {
            case LogType.Error:
                bw.Write($"[{DateTime.Now.ToString("hh:mm:ss")}]{type}\nMessage:\n{logString}\nStacktrace:\n{stackTrace}\n");
                break;
            case LogType.Assert:
                bw.Write($"[{DateTime.Now.ToString("hh:mm:ss")}]{type}\nMessage:\n{logString}\nStacktrace:\n{stackTrace}\n");
                break;
            case LogType.Warning:
                return;
            case LogType.Log:
                bw.Write($"[{DateTime.Now.ToString("hh:mm:ss")}]{type}\nMessage:\n{logString}\nStacktrace:\n{stackTrace}\n");
                break;
            case LogType.Exception:
                bw.Write($"[{DateTime.Now.ToString("hh:mm:ss")}]{type}\nMessage:\n{logString}\nStacktrace:\n{stackTrace}\n");
                break;
            default:
                break;
        }
        sock?.Send(packet);
        Flush();
    }


    private static void InitStream()
    {
        ms = new MemoryStream(packet);
        bw = new BinaryWriter(ms);
    }

    private static void InitSocketAndConnect()
    {
        if (!Instance)
        {
            Debug.Log("Not Found Logger instance");
            return;
        }
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IPAddress ip = IPAddress.Parse(Instance.serverIp);
        IPEndPoint ep = new IPEndPoint(ip, Instance.serverPort);
        sock.Connect(ep);
        Application.quitting += Disconnect;
        Log($"Connected To Echo Server {ep}");
    }

    private static void Flush()
    {
        ms.SetLength(0);
    }

    public static void Disconnect()
    {
        bw.Write($"exit");
        sock?.Send(packet);
        sock?.Close();
        sock?.Dispose();
        ms?.Dispose();
        bw?.Dispose();
    }
}