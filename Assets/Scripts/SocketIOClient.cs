using System.Net.WebSockets;
using System.Threading.Tasks;
using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class SocketIOClient : IDisposable
{
    public enum MessageType
    {
        Opened,
        Ping = 2,
        Pong,
        Connected = 40,
        Disconnected,
        EventMessage,
        AckMessage,
        ErrorMessage,
        BinaryMessage,
        BinaryAckMessage
    }

    public class Message
    {
        private string rawText;
        public string RawText { get => rawText; }
        private string jsonTxt = "";
        public JToken Json 
        { 
            get 
            {
                if (string.IsNullOrEmpty(jsonTxt))
                {
                    // 42[\"hi\",{\"a\":\"javascript object\",\"b\":2},\"string\"]
                    jsonTxt = rawText;
                    var startIndex = jsonTxt.IndexOf(',') + 1;
                    // {\"a\":\"javascript object\",\"b\":2},\"string\"]"
                    jsonTxt = jsonTxt.Substring(startIndex, jsonTxt.Length - startIndex);
                    // {\"msg\":[{\"a\":\"javascript object\",\"b\":2},\"string\"]}
                    jsonTxt = "{\"msg\":[" + jsonTxt + "}";
                }
                return JToken.Parse(jsonTxt)["msg"]; 
            } 
        }

        public Message(string rawText)
        {
            this.rawText = rawText;
        }
    }

    private ClientWebSocket ws;
    private Uri serverUri;
    private Dictionary<string, Action<Message>> eventMap = new Dictionary<string, Action<Message>>();
    public event Action<SocketIOClient> OnConnected;

    public SocketIOClient(Uri serverUri)
    {
        var uriBuilder = new System.Text.StringBuilder();
        uriBuilder.Append("ws://");
        uriBuilder.Append(serverUri.Host);
        if (!serverUri.IsDefaultPort)
        {
            uriBuilder.Append(":").Append(serverUri.Port);
        }
        uriBuilder.Append("/socket.io").
            Append("/?EIO=").
            Append(4).
            Append("&transport=").
            Append("websocket");
        this.serverUri = new Uri(uriBuilder.ToString());
    }

    public async Task ConnectWebSocket()
    {
        ws = new ClientWebSocket();
        await ws.ConnectAsync(serverUri, System.Threading.CancellationToken.None).ConfigureAwait(false);
        _ = await Task.Factory.StartNew(ListenAsync, TaskCreationOptions.LongRunning);
    }

    private async Task ListenAsync()
    {
        while (true)
        {
            int bufferSize = 0;
            var buffer = new byte[bufferSize];
            int currentIdx = 0;
            WebSocketReceiveResult result = null;

            while (ws.State == WebSocketState.Open)
            {
                int chunkedBufferSize = 8 * 1024;
                var chunkedBuffer = new byte[chunkedBufferSize];

                try
                {
                    result = await ws.ReceiveAsync(
                        new ArraySegment<byte>(chunkedBuffer),
                        System.Threading.CancellationToken.None).ConfigureAwait(false);
                    var freeSize = buffer.Length - currentIdx;
                    if (freeSize < result.Count)
                    {
                        Array.Resize(ref buffer, buffer.Length + result.Count);
                    }
                    // chunkedBuffer에서 result.Count 만큼 모든 데이터를 buffer의 currentIdx에 복사함
                    Buffer.BlockCopy(chunkedBuffer, 0, buffer, currentIdx, result.Count);
                    currentIdx += result.Count;

                    if (result.EndOfMessage)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                    break;
                }
            }

            if (result == null)
            {
                break;
            }

            Debug.Log($"Packet Received, MessageType: {result.MessageType}");
            int eio = 4;
            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                    string text = System.Text.Encoding.UTF8.GetString(buffer, 0, currentIdx);
                    Debug.Log(text);
                    // SocketIO 핸드쉐이크, 소켓이 열리면 Connected를 날려줘야한다.
                    if (IsOpendMessage(text))
                    {
                        var bytesForConnected = System.Text.Encoding.UTF8.GetBytes(MessageType.Connected.GetHashCode().ToString());
                        await SendAsync(WebSocketMessageType.Text, bytesForConnected, System.Threading.CancellationToken.None);
                    }
                    else if (IsConnetedMessage(text))
                    {
                        OnConnected(this);
                    }
                    // to keep track of establishment of connection
                    else if (IsPingMessage(text))
                    {
                        var bytesForPong = System.Text.Encoding.UTF8.GetBytes(MessageType.Pong.GetHashCode().ToString());
                        await SendAsync(WebSocketMessageType.Text, bytesForPong, System.Threading.CancellationToken.None);
                    }
                    else if (IsEventMessage(text))
                    {
                        // "hi"
                        var eventId = text.Substring(
                            text.IndexOf('\"'),
                            text.IndexOf(',') - text.IndexOf('\"')).
                            Trim('\"');
                        eventMap[eventId](new Message(text));
                    }
                    break;
                case WebSocketMessageType.Binary:
                    byte[] bytes;
                    if (eio == 3)
                    {
                        bytes = new byte[currentIdx - 1];
                        Buffer.BlockCopy(buffer, 1, bytes, 0, bytes.Length);
                    }
                    else
                    {
                        bytes = new byte[currentIdx];
                        Buffer.BlockCopy(buffer, 0, bytes, 0, bytes.Length);
                    }
                    break;
                case WebSocketMessageType.Close:
                    Debug.Log("WebSocket Closed");
                    break;
                default:
                    break;
            }
        }
        Debug.Log("End of Websocket Task");
    }

    private static bool IsOpendMessage(string msg)
    {
        return msg.StartsWith(MessageType.Opened.GetHashCode().ToString());
    }
    private static bool IsConnetedMessage(string msg)
    {
        return msg.StartsWith(MessageType.Connected.GetHashCode().ToString());
    }
    private static bool IsPingMessage(string msg)
    {
        return msg.StartsWith(MessageType.Ping.GetHashCode().ToString());
    }
    private static bool IsEventMessage(string msg)
    {
        return msg.StartsWith(MessageType.EventMessage.GetHashCode().ToString());
    }

    public void On(string eventId, Action<Message> handler)
    {
        if (!eventMap.ContainsKey(eventId))
        {
            eventMap.Add(eventId, handler);
        }
    }

    public Task EmitAsync(string eventId, params object[] datas)
    {
        var socketIoFormatBuilder = new System.Text.StringBuilder();
        // "42["
        socketIoFormatBuilder.Append("42[");
        // "42[\"eventId\""
        socketIoFormatBuilder.Append(JsonConvert.SerializeObject(eventId));
        foreach (var data in datas)
        {
            // "42[\"eventId\","
            socketIoFormatBuilder.Append(",");
            // "42[\"eventId\",\"data[0]"\""
            socketIoFormatBuilder.Append(JsonConvert.SerializeObject(data));
        }
        // "42[\"eventId\",\"data[0]"\, {\"data[1]\"}]"
        socketIoFormatBuilder.Append("]");
        string jsonString = socketIoFormatBuilder.ToString();
        return SendAsync(WebSocketMessageType.Text, System.Text.Encoding.UTF8.GetBytes(jsonString), System.Threading.CancellationToken.None);
    }

    private async Task SendAsync(WebSocketMessageType type, byte[] bytes, System.Threading.CancellationToken cancellationToken)
    {
        int chunkedBufferSize = 8 * 1024;
        int pages = (int)Math.Ceiling(bytes.Length * 1.0 / chunkedBufferSize);
        for (int i = 0; i < pages; i++)
        {
            int currentIdx = i * chunkedBufferSize;
            int length = chunkedBufferSize;
            if (currentIdx + length > bytes.Length)
            {
                length = bytes.Length - currentIdx;
            }
            byte[] chunkedBuffer = new byte[length];
            // bytes의 currentIdx부터 chunk 사이즈 만큼 데이터를 chunk에 복사함.
            Buffer.BlockCopy(bytes, currentIdx, chunkedBuffer, 0, chunkedBuffer.Length);
            bool endOfMessage = pages - 1 == i;
            await ws.SendAsync(new ArraySegment<byte>(chunkedBuffer), type, endOfMessage, cancellationToken).ConfigureAwait(false);
            Debug.Log($"sended: {System.Text.Encoding.UTF8.GetString(chunkedBuffer)}");
        }
    }

    public void Dispose()
    {
        ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", System.Threading.CancellationToken.None);
    }
}
