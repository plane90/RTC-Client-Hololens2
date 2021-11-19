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

    public event Action<byte[]> FrameEncodedArrived;

    public FrameCapture(Action<byte[]> action)
    {
        FrameEncodedArrived = action;
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
        Logger.Log("RegisterFrameReceiverViaFrameReader");
        _mediaCapture.FrameSources.TryGetValue(_sourceInfo.Id, out _mediaFrameSource);
        var frameReader = await _mediaCapture.CreateFrameReaderAsync(_mediaFrameSource);
        frameReader.FrameArrived += OnFrameArrived;
        Logger.Log("RegisterFrameReceiverViaFrameReader Done ");
    }
    
    private async void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        Logger.Log("OnFrameArrived");
        var frameWrapper = sender.TryAcquireLatestFrame();
        var frame = frameWrapper.VideoMediaFrame;
        var bitmap = frame.SoftwareBitmap;
        var buffer = await EncodedBytes(bitmap, BitmapEncoder.JpegEncoderId);
        FrameEncodedArrived(buffer);
        foreach (var dc in PeerManager.DataChannels)
        {
            //dc.SendMessage()
        }
    }

    private async Task<byte[]> EncodedBytes(SoftwareBitmap soft, Guid encoderId)
    {
        Logger.Log("EncodedBytes");
        byte[] array = null;

        // First: Use an encoder to copy from SoftwareBitmap to an in-mem stream (FlushAsync)
        // Next:  Use ReadAsync on the in-mem stream to get byte[] array

        using (var ms = new InMemoryRandomAccessStream())
        {
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, ms);
            encoder.SetSoftwareBitmap(soft);

            try
            {
                await encoder.FlushAsync();
            }
            catch (Exception ex) { return new byte[0]; }

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
            Logger.Log("MrcVideoEffectDefinition created");
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