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
#else
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
#endif

public class FrameCapture
{

#if ENABLE_WINMD_SUPPORT
    private MediaCaptureVideoProfile _videoProfile;
    private Windows.Media.Capture.Frames.MediaFrameSourceInfo _sourceInfo;
    private Windows.Media.Capture.MediaCapture _mediaCapture;
    private Windows.Media.Capture.Frames.MediaFrameSource _mediaFrameSource;
    private MediaFrameSourceKind sourceKind = MediaFrameSourceKind.Color;
    private MediaStreamType mediaStreamType = MediaStreamType.VideoRecord;
    private float timer = 0f;
    private BufferScheduler bufferScheduler;

    public event Action<byte[]> FrameEncodedArrived;
    public event Action<BufferScheduler> OnReadyToReceiveFrame;

    public FrameCapture(Action<BufferScheduler> OnReadyToReceiveFrame)
    {
        this.OnReadyToReceiveFrame = OnReadyToReceiveFrame;
    }

    public async void Run()
    {
        //textMesh = FindObjectOfType<TextMeshPro>();
        try
        {
            Logger.Log("ENABLE_WINMD_SUPPROT");
            await InitVideoSource();
            await InitMediaCapture();
            await RegisterFrameReceiverViaFrameReader();
            bufferScheduler = new BufferScheduler();
            OnReadyToReceiveFrame(bufferScheduler);
            Logger.Log("BufferScheduler Created");
        }
        catch (Exception e)
        {
            Logger.Log(e.Message, e.StackTrace, LogType.Exception);
        }
    }

    private async Task InitVideoSource()
    {
        try
        {
            Logger.Log("InitVideoSource");
            _videoProfile = await GetVideoProfile();
            if (_videoProfile == null)
            {
                Logger.Log($"Fail::GetVideoProfile, _videoProfile is null");
                return;
            }

            foreach (var frameSourceInfo in _videoProfile.FrameSourceInfos)
            {
                Logger.Log($"FrameSourceInfos: {frameSourceInfo.SourceKind}");
            }

            foreach (var frameSourceInfo in _videoProfile.FrameSourceInfos)
            {
                if (frameSourceInfo.SourceKind == sourceKind && frameSourceInfo.MediaStreamType == mediaStreamType)
                {
                    _sourceInfo = frameSourceInfo;
                }
            }
        }
        catch (Exception e)
        {
            Logger.Log(e.Message, e.StackTrace, LogType.Exception);
        }
    }

    private async Task<MediaCaptureVideoProfile> GetVideoProfile()
    {
        Logger.Log("GetVideoProfile");
        var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
        foreach (var device in devices)
        {
            var videoProfiles = MediaCapture.FindKnownVideoProfiles(device.Id, KnownVideoProfile.VideoConferencing);
            if (videoProfiles.Count > 0)
            {
                return videoProfiles[0];
            }
        }
        return null;
    }

    private async Task InitMediaCapture()
    {
        try
        {
            Logger.Log("Init MediaCapture");
            _mediaCapture = new MediaCapture();
            var initSetting = new MediaCaptureInitializationSettings()
            {
                SourceGroup = _sourceInfo.SourceGroup,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MediaCategory = MediaCategory.Media,
                VideoProfile = _videoProfile,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
            };
            await _mediaCapture.InitializeAsync(initSetting);
            var mrcVideoEffectDefinition = new MrcVideoEffectDefinition();
            var result = await _mediaCapture.AddVideoEffectAsync(mrcVideoEffectDefinition, MediaStreamType.VideoRecord);
            if (result == null)
            {
                Logger.Log("AddVideoEffectAsync Fail");
            }
            Logger.Log("Effect Added !");
            Logger.Log("Done");
        }
        catch (Exception e)
        {
            Logger.Log(e.Message, e.StackTrace, LogType.Exception);
        }
    }

    private async Task RegisterFrameReceiverViaFrameReader()
    {
        Logger.Log("Create FrameReader & Register Event");
        _mediaCapture.FrameSources.TryGetValue(_sourceInfo.Id, out _mediaFrameSource);
        var frameReader = await _mediaCapture.CreateFrameReaderAsync(_mediaFrameSource);
        frameReader.FrameArrived += OnFrameArrived;
        MediaFrameReaderStartStatus status = await frameReader.StartAsync();
        if (status == MediaFrameReaderStartStatus.Success)
        {
            Logger.Log($"Started {_mediaFrameSource.Info.SourceKind} reader.");
        }
        else
        {
            Logger.Log($"Unable to start {_mediaFrameSource.Info.SourceKind} reader. Error: {status}");
        }
        Logger.Log("Done ");
    }

    private async void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        BufferScheduler.FrameBuffer fb = new BufferScheduler.FrameBuffer();
        bufferScheduler.AddSource(fb);
        try
        {
            Logger.Log($"Media Source Arrived ! threadID:{System.Threading.Thread.CurrentThread.ManagedThreadId}");
            var frameWrapper = sender.TryAcquireLatestFrame();
            var vmf = frameWrapper.VideoMediaFrame;
            Logger.Log($"Video Frame Rate: {vmf.VideoFormat.MediaFrameFormat.FrameRate}");
            var bitmap = SoftwareBitmap.Convert(vmf.SoftwareBitmap, BitmapPixelFormat.Rgba8);
            fb.frame = await EncodedBytes(bitmap, BitmapEncoder.JpegEncoderId);
            fb.isReady = true;
        }
        catch (Exception e)
        {
            fb.isFail = true;
            Logger.Log(e.Message, e.StackTrace, LogType.Exception);
        }
    }

    private async Task<byte[]> EncodedBytes(SoftwareBitmap bitmap, Guid encoderId)
    {
        byte[] array = null;
        // First: Use an encoder to copy from SoftwareBitmap to an in-mem stream (FlushAsync)
        // Next:  Use ReadAsync on the in-mem stream to get byte[] array

        using (var ms = new InMemoryRandomAccessStream())
        {
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, ms);
            encoder.SetSoftwareBitmap(bitmap);
            encoder.BitmapTransform.ScaledWidth = 640;
            encoder.BitmapTransform.ScaledHeight = 480;
            try
            {
                await encoder.FlushAsync();
            }
            catch (Exception e)
            {
                Logger.Log(e.Message, e.StackTrace, LogType.Exception);
                return new byte[0];
            }

            array = new byte[ms.Size];
            await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);
        }
        return array;
    }


    public sealed class MrcVideoEffectDefinition : IVideoEffectDefinition
    {
        public string ActivatableClassId => "Windows.Media.MixedRealityCapture.MixedRealityCaptureVideoEffect";

        public IPropertySet Properties { get; }

        public MrcVideoEffectDefinition()
        {
            Logger.Log("MrcVideoEffectDefinition Created");
            Properties = new PropertySet
                {
                    {"StreamType", MediaStreamType.VideoRecord},
                    {"HologramCompositionEnabled", true},
                    {"RecordingIndicatorEnabled", true},
                    {"VideoStabilizationEnabled", false},
                    {"VideoStabilizationBufferLength", 0},
                    {"GlobalOpacityCoefficient", 0.9f},
                    {"BlankOnProtectedContent", false},
                    {"ShowHiddenMesh", false},
                    //{"PreferredHologramPerspective", MixedRealityCapturePerspective.PhotoVideoCamera},    // fatal error
                    //{"OutputSize", 0},    // fatal error
                };
        }
    }
#endif
}