using System;
using System.Collections;
using UnityEngine;

public class DeviceChange : MonoBehaviour
{
    public event Action<Vector2> OnResolutionChange;
    public event Action<DeviceOrientation> OnOrientationChange;
    public float CheckDelay = 0.5f; // How long to wait until we check again.

    private Vector2Int _resolution; // Current Resolution
    private DeviceOrientation _orientation; // Current Device Orientation
    private bool _isAlive = true; // Keep this script running?
    
    private static DeviceChange _instance = null;

    public static DeviceChange Instance
    {
        get
        {
            if (_instance != null)
            {
                return _instance;
            }
            
            _instance = FindObjectOfType<DeviceChange>();

            if (_instance != null)
            {
                return _instance;
            }

            Type type = typeof(DeviceChange);
            GameObject obj = new GameObject(nameof(DeviceChange), type);
            _instance = obj.GetComponent(type) as DeviceChange;

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            return;
        }

        if (_instance != this)
        {
            Debug.LogWarning("A DeviceChange object has been created twice or more. This instance will be destroyed.");
            Destroy(this);
        }
    }

    private void Start()
    {
        StartCoroutine(CheckForChange());
    }

    private IEnumerator CheckForChange()
    {
        _resolution = new Vector2Int(Screen.width, Screen.height);
        _orientation = Input.deviceOrientation;

        while (_isAlive)
        {
            // Check for a Resolution Change
            if (_resolution.x != Screen.width || _resolution.y != Screen.height)
            {
                _resolution = new Vector2Int(Screen.width, Screen.height);
                OnResolutionChange?.Invoke(_resolution);
            }

            // Check for an Orientation Change
            switch (Input.deviceOrientation)
            {
                case DeviceOrientation.Unknown: // Ignore
                case DeviceOrientation.FaceUp: // Ignore
                case DeviceOrientation.FaceDown: // Ignore
                    break;
                
                default:
                    if (_orientation != Input.deviceOrientation)
                    {
                        _orientation = Input.deviceOrientation;
                        OnOrientationChange?.Invoke(_orientation);
                    }

                    break;
            }

            yield return new WaitForSeconds(CheckDelay);
        }
    }

    private void OnDestroy()
    {
        _isAlive = false;
    }
}
