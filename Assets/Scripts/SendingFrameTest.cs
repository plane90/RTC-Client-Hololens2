using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendingFrameTest : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    private FrameCapture frameCapture;
    private float timer = 0f;
    BufferScheduler bs;

    private void Start()
    {
        try
        {
            Logger.Log("Try to Run FrameCapture");
            frameCapture = new FrameCapture(OnFrameEncodedArrived);
            frameCapture.Run();
            Logger.Log("FrameCapture Run");
        }
        catch (System.Exception e)
        {
            Logger.Log(e.Message, e.StackTrace, LogType.Exception);
        }
    }

    private async void Update()
    {
        if (bs != null)
        {
            var frame = await bs.GetLatestFrame();
            if (frame != null)
            {
                Logger.Log("Sending Frame");
                Logger.Frame(frame);
            }
        }
    }

    private void OnFrameEncodedArrived(BufferScheduler bs)
    {
        Logger.Log("Ready to Receive Frame ");
        this.bs = bs;
    }
#endif
}
