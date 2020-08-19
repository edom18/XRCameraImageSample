using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class CameraCaptureTest : MonoBehaviour
{
    [SerializeField] private ARCameraManager _cameraManager = null;
    [SerializeField] private GameObject _target = null;
    [SerializeField] private Texture2D _sampleTexture = null;

    private Texture2D _texture = null;
    private Renderer _renderer = null;
    private Material _material = null;

    private void Start()
    {
        Debug.Log(">>>>>>>>> START <<<<<<<<<<");
        _cameraManager.frameReceived += OnCameraFrameReceived;
        _renderer = _target.GetComponent<Renderer>();
        
        _material = _renderer.material;
        _material.mainTexture = _sampleTexture;
    }

    private void Update()
    {
        //if (Input.touchCount > 0)
        //{
        //    Touch touch = Input.GetTouch(0);
        //    if (touch.phase == TouchPhase.Began)
        //    {
        //        Capture();
        //    }
        //}
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        RefreshCameraFeedTexture();
    }

    private void RefreshCameraFeedTexture()
    {
        if (!_cameraManager.TryGetLatestImage(out XRCameraImage cameraImage))
        {
            Debug.Log("Failed to get the last image.");
            return;
        }

        if (_texture == null || _texture.width != cameraImage.width || _texture.height != cameraImage.height)
        {
            if (_texture != null)
            {
                DestroyImmediate(_texture);
            }
            
            _texture = new Texture2D(cameraImage.width, cameraImage.height, TextureFormat.RGBA32, false);
            
            if(_texture != null)
            {
                _material.SetFloat("_UVMultiplierLandScape", CalculateUVMultiplierLandScape(_texture));
                _material.SetFloat("_UVMultiplierPortrait", CalculateUVMultiplierPortrait(_texture));
            }
        }

        CameraImageTransformation imageTransformation = (Input.deviceOrientation == DeviceOrientation.LandscapeRight) ?
                                    CameraImageTransformation.MirrorY :
                                    CameraImageTransformation.MirrorX;
        XRCameraImageConversionParams conversionParams = new XRCameraImageConversionParams(cameraImage, TextureFormat.RGBA32, imageTransformation);

        NativeArray<byte> rawTextureData = _texture.GetRawTextureData<byte>();

        try
        {
            unsafe
            {
                cameraImage.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
            }
        }
        finally
        {
            cameraImage.Dispose();
        }

        _texture.Apply();
        _renderer.material.mainTexture = _texture;
    }

    private float CalculateUVMultiplierLandScape(Texture2D cameraTexture)
    {
        float screenAspect = (float) Screen.width / (float) Screen.height;
        float cameraTextureAspect = (float) cameraTexture.width / (float) cameraTexture.height;
        return screenAspect / cameraTextureAspect;
    }
    
    private float CalculateUVMultiplierPortrait(Texture2D cameraTexture)
    {
        float screenAspect = (float) Screen.height / (float) Screen.width;
        float cameraTextureAspect = (float) cameraTexture.width / (float) cameraTexture.height;
        return screenAspect / cameraTextureAspect;
    }

    // This function refere the Unity document.
    // https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.0/manual/cpu-camera-image.html
    //unsafe private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    //{
    //    Debug.Log("-- OnCameraFrameReceived --");

    //    XRCameraImage image;
    //    XRCameraSubsystem cameraSubsystem = _cameraManager.subsystem;

    //    if (cameraSubsystem == null)
    //    {
    //        return;
    //    }

    //    if (!cameraSubsystem.TryGetLatestImage(out image))
    //    {
    //        return;
    //    }

    //    Debug.Log("Will convert the image.");

    //    var conversionParams = new XRCameraImageConversionParams
    //    {
    //        // Get the entire image.
    //        inputRect = new RectInt(0, 0, image.width, image.height),
    //        outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
    //        outputFormat = TextureFormat.RGBA32,
    //        transformation = CameraImageTransformation.MirrorY,
    //    };

    //    int size = image.GetConvertedDataSize(conversionParams);
    //    var buffer = new NativeArray<byte>(size, Allocator.Temp);
    //    image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);
    //    image.Dispose();

    //    if (_texture != null)
    //    {
    //        DestroyImmediate(_texture);
    //    }

    //    _texture = new Texture2D(conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, conversionParams.outputFormat, false);
    //    _texture.LoadRawTextureData(buffer);
    //    _texture.Apply();
    //    _renderer.material.mainTexture = _texture;

    //    buffer.Dispose();
    //}
}

