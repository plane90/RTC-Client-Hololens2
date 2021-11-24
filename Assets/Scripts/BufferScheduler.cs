#if ENABLE_WINMD_SUPPORT
using System;
using System.Threading.Tasks;
using UnityEngine;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Effects;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using System.Collections.Generic;
#else
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
#endif

public class BufferScheduler
{
#if ENABLE_WINMD_SUPPORT
    public class FrameBuffer
    {
        public bool isReady = false;
        public bool isFail = false;
        public byte[] frame;
    }

    private Queue<FrameBuffer> sq = new Queue<FrameBuffer>();

    public void AddSource(FrameBuffer fb)
    {
        sq.Enqueue(fb);
    }

    public async Task<byte[]> GetLatestFrame()
    {
        try
        {
            var source = sq.Dequeue();
            await Task.Run(() =>
            {
                while (true)
                {
                    if (source.isReady || source.isFail)
                    {
                        break;
                    }
                }
            });
            return source.frame;
        }
        catch (Exception e)
        {
            Logger.Log(e.Message, e.StackTrace, LogType.Exception);
            return null;
        }
    }

#endif
}