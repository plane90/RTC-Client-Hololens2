using System;
using UnityEngine;
using Microsoft.MixedReality.WebRTC;
using System.Text;
using System.Net.WebSockets;

public class Test : MonoBehaviour
{
    public string signallingServerUri = "http://localhost:14002/";
    public string roomId = "a";

    async void Start()
    {
        Debug.Log($"i'm main Thread,  {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        if (string.IsNullOrEmpty(signallingServerUri))
        {
            signallingServerUri = "http://localhost:14002/";
        }
        var wsClient = new SocketIOClient(new Uri(signallingServerUri));
        wsClient.OnConnected += async (sock) =>
        {
            await sock.EmitAsync("join", "a");
        };
        wsClient.On("hi", (msg) =>
        {
            Debug.Log(msg.Json);
            Debug.Log(msg.Json[0]);
            Debug.Log(msg.Json[0]["a"]);
            Debug.Log(msg.Json[0]["b"]);
            Debug.Log(msg.Json[1]);
        });
        wsClient.On("noti_join", async (msg) =>
        {
            var socketId = msg.Json[0].ToString();
            Debug.Log($"{socketId},  {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            await PeerManager.CreatePeerConnectionAndDataChannel(socketId);
            Debug.Log($"try to get peerConnection via PeerManager {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            var pc = PeerManager.GetPeerConnection(socketId);
            pc.CreateOffer();

            pc.Connected += () =>
            {
                Debug.Log("pc connected");
            };
            pc.DataChannelAdded += (DataChannel dataChannel) =>
            {
                Debug.Log($"DataChannel added, ID: {dataChannel.ID}, Label: {dataChannel.Label}, State: {dataChannel.State}");
            };
            pc.LocalSdpReadytoSend += async (SdpMessage sdpMsg) =>
            {
                Debug.Log($"[send] {sdpMsg.Type}\n");
                await wsClient.EmitAsync("offer", new { sdp = sdpMsg.Content, socketId }, roomId);
            };
            pc.IceCandidateReadytoSend += (IceCandidate iceCandidate) =>
            {
                Debug.Log($"[send] ice {iceCandidate.Content}");
                var ice = new { iceCandidate.SdpMid, iceCandidate.SdpMlineIndex, candidate = iceCandidate.Content, socketId };
                wsClient.EmitAsync("ice", ice, roomId, socketId);
            };
            var dc = PeerManager.GetDataChannel(socketId);
            dc.StateChanged += () =>
            {
                Debug.Log($"State Changed to {dc.State}");
                dc.SendMessage(Encoding.UTF8.GetBytes("hello"));
            };
            dc.MessageReceived += (byte[] obj) =>
            {
                string str = Encoding.UTF8.GetString(obj);
                Debug.Log($"[received] Message : {str}");
            };
        });

        wsClient.On("offer", (offer) =>
        {
            // Do Nothing
        });

        wsClient.On("answer", (msg) =>
        {
            string sdp = msg.Json[0]["sdp"].ToString();
            Debug.Log($"[received] answer \n {sdp}");
            var sdpAnswer = new SdpMessage();
            sdpAnswer.Type = SdpMessageType.Answer;
            sdpAnswer.Content = sdp;
            string socketId = msg.Json[0]["socketId"].ToString();
            var pc = PeerManager.GetPeerConnection(socketId);
            pc.SetRemoteDescriptionAsync(sdpAnswer);
        });

        wsClient.On("ice", (msg) =>
        {
            Debug.Log($"[received] ice \n");
            var iceCandidate = new IceCandidate();
            iceCandidate.Content = msg.Json[0]["Content"].ToString();
            iceCandidate.SdpMid = msg.Json[0]["SdpMid"].ToString();
            iceCandidate.SdpMlineIndex = int.Parse(msg.Json[0]["SdpMlineIndex"].ToString());
            string socketId = msg.Json[0]["socketId"].ToString();
            var pc = PeerManager.GetPeerConnection(socketId);
            pc.AddIceCandidate(iceCandidate);
        });

        await wsClient.ConnectWebSocket();
        Debug.Log("---------disconnected");
    }

    public void Send(string msg)
    {
        if (PeerManager.PeerDataCount == 0)
        {
            return;
        }
        foreach (var peerData in PeerManager.PeerDatas)
        {
            var dc = peerData.dataChannel;
            var pc = peerData.peerConnection;

            if (dc != null && dc.State == DataChannel.ChannelState.Open)
            {
                switch (msg)
                {
                    case "disconnect":
                        pc.Close();
                        break;
                    default:
                        dc.SendMessage(Encoding.UTF8.GetBytes(msg));
                        break;
                }
            }
        }
    }
}
