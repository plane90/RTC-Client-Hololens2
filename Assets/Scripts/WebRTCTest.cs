using System;
using UnityEngine;
using Microsoft.MixedReality.WebRTC;
using System.Text;
using System.Net.WebSockets;

public class WebRTCTest : MonoBehaviour
{
    public string signallingServerUri = "http://localhost:14002/";
    public string roomId = "a";

    private SocketIOClient wsClient;

    private void OnDestroy()
    {
        wsClient?.Dispose();
        PeerManager.CloseAll();
    }

    async void Start()
    {
        Logger.Log("hi");
        Logger.Log($"i'm main Thread,  {System.Threading.Thread.CurrentThread.ManagedThreadId}");
        if (string.IsNullOrEmpty(signallingServerUri))
        {
            signallingServerUri = "http://localhost:14002/";
        }
        wsClient = new SocketIOClient(new Uri(signallingServerUri));
        wsClient.OnConnected += async (sock) =>
        {
            await sock.EmitAsync("join", "a");
        };
        wsClient.OnDisconnected += (sock) =>
        {
            Logger.Log("websocket disconnected");
        };
        wsClient.On("hi", (msg) =>
        {
        });
        wsClient.On("noti_join", async (msg) =>
        {
            var socketId = msg.Json[0].ToString();
            Logger.Log($"noti_join\t{socketId},  {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            await PeerManager.CreatePeerConnectionAndDataChannel(socketId);
            Logger.Log($"noti_join\ttry to get peerConnection via PeerManager {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            var pc = PeerManager.GetPeerConnection(socketId);
            pc.CreateOffer();

            pc.Connected += () =>
            {
                Logger.Log("pc connected");
            };
            pc.DataChannelAdded += (DataChannel dataChannel) =>
            {
                Logger.Log($"DataChannel added, ID: {dataChannel.ID}, Label: {dataChannel.Label}, State: {dataChannel.State}");
            };
            pc.LocalSdpReadytoSend += async (SdpMessage sdpMsg) =>
            {
                Logger.Log($"[send] {sdpMsg.Type}\n");
                await wsClient.EmitAsync("offer", new { sdp = sdpMsg.Content, socketId }, roomId);
            };
            pc.IceCandidateReadytoSend += (IceCandidate iceCandidate) =>
            {
                Logger.Log($"[send] ice {iceCandidate.Content}");
                var ice = new { iceCandidate.SdpMid, iceCandidate.SdpMlineIndex, candidate = iceCandidate.Content, socketId };
                wsClient.EmitAsync("ice", ice, roomId);
            };
            var dc = PeerManager.GetDataChannel(socketId);
            dc.StateChanged += () =>
            {
                Logger.Log($"State Changed to {dc.State} {socketId}");
                if (dc.State == DataChannel.ChannelState.Open)
                {
                    dc.SendMessage(Encoding.UTF8.GetBytes("hello"));
                }
            };
            dc.MessageReceived += (byte[] obj) =>
            {
                string str = Encoding.UTF8.GetString(obj);
                Logger.Log($"[received] Message : {str}");
            };
        });

        wsClient.On("offer", (offer) =>
        {
            // Do Nothing
        });

        wsClient.On("answer", (msg) =>
        {
            string sdp = msg.Json[0]["sdp"].ToString();
            Logger.Log($"[received] answer \n {sdp}");
            var sdpAnswer = new SdpMessage();
            sdpAnswer.Type = SdpMessageType.Answer;
            sdpAnswer.Content = sdp;
            string socketId = msg.Json[0]["socketId"]?.ToString();
            var pc = PeerManager.GetPeerConnection(socketId);
            pc.SetRemoteDescriptionAsync(sdpAnswer);
            Logger.Log($"answer\tSet Remote Done\n");
        });

        wsClient.On("ice", (msg) =>
        {
            Logger.Log($"[received] ice \n");
            var iceCandidate = new IceCandidate();
            iceCandidate.Content = msg.Json[0]["Content"]?.ToString();
            iceCandidate.SdpMid = "0";
            iceCandidate.SdpMlineIndex = 0;
            var socketId = msg.Json[0]["socketId"]?.ToString();
            PeerManager.GetPeerConnection(socketId).AddIceCandidate(iceCandidate);
            Logger.Log($"ice\tAdd Ice Done\n");
        });

        await wsClient.ConnectWebSocket();
        Logger.Log($"------Listening {System.Threading.Thread.CurrentThread.ManagedThreadId}");
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
