using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Threading;


public class Logger : ScriptableObject
{
    /* instance member */
    [SerializeField] private string serverIp = "10.112.58.138";
    [SerializeField] private int serverPort = 10004;

    public static Socket sock;
    private static Logger instance;
    private static SemaphoreSlim connectSemaphore = new SemaphoreSlim(1, 1);
    private static SemaphoreSlim sendSemaphore = new SemaphoreSlim(1, 1);

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

    private void OnEnable()
    {
        Connect();
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
        byte[] packet = new byte[8 * 1024];

        using (MemoryStream ms = new MemoryStream(packet))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write($"0");
            bw.Write($"{type.GetHashCode()}");
            bw.Write($"{DateTime.Now.ToString("hh:mm:ss")}");
            bw.Write($"{logString}");
            if (string.IsNullOrEmpty(stackTrace))
            {
                stackTrace += $"{methodName} (at {fileName}:{lineNo})";
            }
            bw.Write($"{stackTrace}");
            bw.Write("");
            Send(packet);
        }
    }

    public static void Frame(byte[] frameEncoded)
    {
        var frameLength = System.Text.Encoding.UTF8.GetBytes(frameEncoded.Length.ToString()).Length + 1;
        byte[] packet = new byte[2 + frameLength + frameEncoded.Length];
        using (MemoryStream ms = new MemoryStream(packet))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            // 0: string 1: binary 2: close
            bw.Write($"1");
            bw.Write($"{frameEncoded.Length}");
            bw.Write(frameEncoded);
            Send(packet);
        }
    }

    private static async void Send(byte[] packet)
    {
        if (sock == null)
        {
            await Connect();
        }
        _ = System.Threading.Tasks.Task.Run(() =>
        {
            sendSemaphore.Wait();
            Thread.Sleep(10);
            sock?.Send(packet);
            Debug.Log($"packet sended id:{Thread.CurrentThread.ManagedThreadId}");
            sendSemaphore.Release();
        });
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
        byte[] packet = new byte[1 * 1024];

        using (MemoryStream ms = new MemoryStream(packet))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write("2");
            bw.Write("");
            sock?.Send(packet);
            sock?.Close();
            sock?.Dispose();
            sock = null;
        }
    }
}