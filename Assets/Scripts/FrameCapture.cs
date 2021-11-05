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

    public event Action<byte[]> FrameEncodedArrived;

    public FrameCapture(Action<byte[]> action)
    {
        FrameEncodedArrived = action;
    }

    private async void Run()
    {
        //textMesh = FindObjectOfType<TextMeshPro>();
        try
        {
            Logger.Log("ENABLE_WINMD_SUPPROT");
            await InitVideoSource();
            await InitMediaCapture();
            await RegisterFrameReceiverViaFrameReader();
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
            Logger.Log("InitMediaCapture");
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
            Logger.Log("MediaCapture Initialized");
        }
        catch (Exception e)
        {
            Logger.Log(e.Message, e.StackTrace, LogType.Exception);
        }
    }

    private async Task RegisterFrameReceiverViaFrameReader()
    {
        _mediaCapture.FrameSources.TryGetValue(_sourceInfo.Id, out _mediaFrameSource);
        var frameReader = await _mediaCapture.CreateFrameReaderAsync(_mediaFrameSource);
        frameReader.FrameArrived += OnFrameArrived;
    }
    
    private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        var frameWrapper = sender.TryAcquireLatestFrame();
        var frame = frameWrapper.VideoMediaFrame;
        var bitmap = frame.SoftwareBitmap;
        //tmap.CopyToBuffer()
        foreach (var dc in PeerManager.DataChannels)
        {
            //dc.SendMessage()
        }
    }

    public sealed class MrcVideoEffectDefinition : IVideoEffectDefinition
    {
        public string ActivatableClassId => "Windows.Media.MixedRealityCapture.MixedRealityCaptureVideoEffect";

        public IPropertySet Properties { get; }

        public MrcVideoEffectDefinition()
        {
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