using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class CaptureXRCamera : MonoBehaviour
{
    [SerializeField] private ARCameraManager _cameraManager = null;
    [SerializeField] private GameObject _target = null;
    [SerializeField] private Texture2D _sampleTexture = null;
    [SerializeField] private Material _transposeMaterial = null;
    [SerializeField] private Text _text = null;

    private Texture2D _texture = null;
    private RenderTexture _previewTexture = null;
    private Renderer _renderer = null;
    private Material _material = null;

    private bool _needsRotate = true;

    private void Start()
    {
        Debug.Log(">>>>>>>>> START <<<<<<<<<<");

        _cameraManager.frameReceived += OnCameraFrameReceived;
        _renderer = _target.GetComponent<Renderer>();

        _material = _renderer.material;
        _material.mainTexture = _sampleTexture;

        _previewTexture = new RenderTexture(_sampleTexture.width, _sampleTexture.height, 0, RenderTextureFormat.BGRA32);
        _previewTexture.Create();

        DeviceChange.Instance.OnOrientationChange += HandleOnOnOrientationChange;

        DisplayInfo();
    }

    private void HandleOnOnOrientationChange(DeviceOrientation orientation)
    {
        ResizePreviewPlane();
        CheckRotation();
        DisplayInfo();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PreviewTexture(_sampleTexture);
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                _needsRotate = !_needsRotate;
            }
        }
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        RefreshCameraFeedTexture();
        DisplayInfo();
    }

    private void CheckRotation()
    {
        _needsRotate = Input.deviceOrientation == DeviceOrientation.Portrait;
    }

    private void DisplayInfo()
    {
        _text.text =
            $"Needs rotate : {_needsRotate.ToString()}\nScreen :  w - {Screen.width.ToString()}, h - {Screen.height.ToString()}\nTexture: w - {_previewTexture.width.ToString()}, h - {_previewTexture.height.ToString()}";
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
        if (_needsRotate)
        {
            Graphics.Blit(texture, _previewTexture, _transposeMaterial);
        }
        else
        {
            Graphics.Blit(texture, _previewTexture);
        }

        _renderer.material.mainTexture = _previewTexture;
    }
}
