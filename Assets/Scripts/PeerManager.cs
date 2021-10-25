﻿using Microsoft.MixedReality.WebRTC;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class PeerData
{
    public string id;
    public PeerConnection peerConnection;
    public DataChannel dataChannel;
}

static class PeerManager
{
    private static Dictionary<string, PeerData> peerDataMap = new Dictionary<string, PeerData>();
    public static int PeerDataCount { get => peerDataMap.Count; }
    public static List<PeerData> PeerDatas { get => peerDataMap.Values.ToList(); }
    public static List<PeerConnection> PeerConnections { get => peerDataMap.Values.Select(x => x.peerConnection).ToList(); }
    public static List<DataChannel> DataChannels { get => peerDataMap.Values.Select(x => x.dataChannel).ToList(); }

    public static async Task CreatePeerConnectionAndDataChannel(string id)
    {
        if (peerDataMap.ContainsKey(id))
        {
            UnityEngine.Debug.Log($"already exist {id}");
            return;
        }
        var pc = new PeerConnection();
        var config = new PeerConnectionConfiguration()
        {
            IceServers = new List<IceServer>
                {
                    new IceServer{ Urls = { "stun:stun.l.google.com:19302" } }
                }
        };
        await pc.InitializeAsync(config);
        var dc = await pc.AddDataChannelAsync("test", false, false);
        peerDataMap.Add(id, new PeerData { id = id, dataChannel = dc, peerConnection = pc });
        UnityEngine.Debug.Log($"Creating PeerConnection done, {System.Threading.Thread.CurrentThread.ManagedThreadId}");
    }

    public static PeerData GetPeerData(string id)
    {
        if (peerDataMap.TryGetValue(id, out PeerData peerData))
        {
            return peerData;
        }
        else
        {
            return null;
        }
    }
    public static PeerConnection GetPeerConnection(string id)
    {
        if (peerDataMap.TryGetValue(id, out PeerData peerData))
        {
            return peerData.peerConnection;
        }
        else
        {
            return null;
        }
    }
    public static DataChannel GetDataChannel(string id)
    {
        if (peerDataMap.TryGetValue(id, out PeerData peerData))
        {
            return peerData.dataChannel;
        }
        else
        {
            return null;
        }
    }
    public static void CloseAll()
    {
        foreach (var peer in PeerDatas)
        {
            try
            {
                UnityEngine.Debug.Log("PeerManager\tTry to close peer");
                peer.peerConnection.RemoveDataChannel(peer.dataChannel);
                UnityEngine.Debug.Log("PeerManager\tRemove DC done");
                //Task.Factory.StartNew(() => peer.peerConnection.Close());
                UnityEngine.Debug.Log("PeerManager\tClose Done");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
            
        }
        peerDataMap.Clear();
    }
}