using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendingFrameTest : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    private FrameCapture frameCapture;
    private float timer = 0f;

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

    private void OnFrameEncodedArrived(byte[] frame)
    {
        Logger.Frame(frame);
        Logger.Log("Send Frame");
    }
#endif
}
