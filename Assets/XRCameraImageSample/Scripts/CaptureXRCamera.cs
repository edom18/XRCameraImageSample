using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class CaptureXRCamera : MonoBehaviour
{
    [SerializeField] private ARCameraManager _cameraManager = null;
    [SerializeField] private GameObject _target = null;
    [SerializeField] private Texture2D _sampleTexture = null;
    [SerializeField] private Material _transposeMaterial = null;

    private Texture2D _texture = null;
    private RenderTexture _previewTexture = null;
    private Renderer _renderer = null;
    private Material _material = null;

    private void Start()
    {
        Debug.Log(">>>>>>>>> START <<<<<<<<<<");

        _cameraManager.frameReceived += OnCameraFrameReceived;
        _renderer = _target.GetComponent<Renderer>();

        _material = _renderer.material;
        _material.mainTexture = _sampleTexture;

        _previewTexture = new RenderTexture(_sampleTexture.width, _sampleTexture.height, 0, RenderTextureFormat.BGRA32);
        _previewTexture.Create();

        DeviceChange.Instance.OnResolutionChange += HandleOnOnResolutionChange;
    }

    private void HandleOnOnResolutionChange(Vector2 obj)
    {
        ResizePreviewPlane();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PreviewTexture(_sampleTexture);
        }
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

            if (_previewTexture != null)
            {
                _previewTexture.Release();
            }

            _texture = new Texture2D(cameraImage.width, cameraImage.height, TextureFormat.RGBA32, false);
            _previewTexture = new RenderTexture(_texture.width, _texture.height, 0, RenderTextureFormat.BGRA32);
            _previewTexture.Create();

            ResizePreviewPlane();
        }

        CameraImageTransformation imageTransformation = (Input.deviceOrientation == DeviceOrientation.LandscapeRight)
            ? CameraImageTransformation.MirrorY
            : CameraImageTransformation.MirrorX;
        XRCameraImageConversionParams conversionParams =
            new XRCameraImageConversionParams(cameraImage, TextureFormat.RGBA32, imageTransformation);

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
        PreviewTexture(_texture);
    }

    private void ResizePreviewPlane()
    {
        float aspect = 1f;
        
        if (Input.deviceOrientation == DeviceOrientation.Portrait)
        {
            aspect = (float)_texture.width / (float)_texture.height;
        }
        else
        {
            aspect = (float)_texture.height / (float)_texture.width;
        }

        _target.transform.localScale = new Vector3(1f, aspect, 1f);
    }

    private void PreviewTexture(Texture2D texture)
    {
        Graphics.Blit(texture, _previewTexture, _transposeMaterial);
        _renderer.material.mainTexture = _previewTexture;
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
