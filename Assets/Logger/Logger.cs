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

    private static byte[] packet = new byte[8 * 1024];
    private static MemoryStream ms;
    private static BinaryWriter bw;

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

    void OnDisable()
    {
        Disconnect();
    }

    public static void Log(string logString, string stackTrace = "", LogType type = LogType.Log)
    {
        Debug.Log(logString);

        if (sock == null)
        {
            InitStream();
            InitSocketAndConnect();
        }
        // 0: string 1: binary 2: close
        bw.Write($"0");
        bw.Write($"{type.GetHashCode()}");
        bw.Write($"{DateTime.Now.ToString("hh:mm:ss")}");
        bw.Write($"{logString}");
        bw.Write($"{stackTrace}");
        bw.Write("");
        sock?.Send(packet);
        Flush();
    }

    public static void Frame()
    {
        if (sock == null)
        {
            InitStream();
            InitSocketAndConnect();
        }
        // 0: string 1: binary 2: close
        bw.Write($"1");
        bw.Write("");
        sock?.Send(packet);
        Flush();
    }


    private static void InitStream()
    {
        ms = new MemoryStream(packet);
        bw = new BinaryWriter(ms);
    }

    private static async void InitSocketAndConnect()
    {
        if (!Instance)
        {
            Debug.Log("Not Found Logger instance");
            return;
        }
        await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var iEP = new IPEndPoint(IPAddress.Parse(Instance.serverIp), Instance.serverPort);
                Debug.Log($"Try To Connect Echo Server {iEP}");
                sock.Connect(iEP);
                Application.quitting += Disconnect;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        });
    }

    private static void Flush()
    {
        ms.Position = 0;
        ms.SetLength(0);
    }

    public static void Disconnect()
    {
        bw?.Write("2");
        bw?.Write("");
        sock?.Send(packet);
        sock?.Close();
        sock?.Dispose();
        ms?.Dispose();
        bw?.Dispose();
    }
}