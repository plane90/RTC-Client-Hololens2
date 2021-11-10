using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SendingFrameTest : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
    private FrameCapture frameCapture;

    private void Start()
    {
        frameCapture = new FrameCapture(OnFrameEncodedArrived);
        frameCapture.Run();
    }

    private void OnFrameEncodedArrived(byte[] frame)
    {
        Logger.Frame(frame);
    }
#endif
}
