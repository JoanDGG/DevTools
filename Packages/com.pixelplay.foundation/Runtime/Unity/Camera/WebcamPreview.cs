using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelPlay.Foundation.Unity.Camera
{
    /// <summary>Starts a webcam and displays its live feed in a UI RawImage.</summary>
    public sealed class WebcamPreview : MonoBehaviour
    {
        /// <summary>Unity event that passes a selected device name or an error message.</summary>
        [Serializable]
        public sealed class StatusEvent : UnityEvent<string> { }

        [Header("Preview")]
        [SerializeField, Tooltip("RawImage that displays the active webcam feed.")] private RawImage preview;
        [SerializeField, Tooltip("Starts the selected camera when this component is enabled.")] private bool playOnEnable = true;
        [SerializeField, Tooltip("Mirrors the displayed preview when the selected device is front-facing.")] private bool mirrorFrontFacingCamera = true;

        [Header("Device")]
        [SerializeField, Tooltip("Exact device name to select; leave empty to use the front-facing preference or first available device.")] private string preferredDeviceName;
        [SerializeField, Tooltip("When no exact device name is set, select a front-facing camera if available.")] private bool preferFrontFacing;
        [SerializeField, Min(16), Tooltip("Requested capture width in pixels; the device may provide another supported size.")] private int requestedWidth = 1280;
        [SerializeField, Min(16), Tooltip("Requested capture height in pixels; the device may provide another supported size.")] private int requestedHeight = 720;
        [SerializeField, Min(1), Tooltip("Requested capture frame rate; the device may provide another rate.")] private int requestedFrameRate = 30;
        [SerializeField, Min(1f), Tooltip("Seconds to wait for the webcam to start before reporting a timeout.")] private float startupTimeoutSeconds = 10f;

        [Header("Events")]
        [SerializeField, Tooltip("Invoked after the camera starts; receives the selected device name.")] private StatusEvent onCameraStarted = new StatusEvent();
        [SerializeField, Tooltip("Invoked when permission, device selection, or startup fails; receives the reason.")] private StatusEvent onError = new StatusEvent();

        private WebCamTexture _webcam;
        private Coroutine _startupCoroutine;
        private Quaternion _previewOriginalRotation;
        private Rect _previewOriginalUvRect;
        private string _currentDeviceName;

        /// <summary>Gets the active stream, or null when the camera is stopped.</summary>
        public WebCamTexture Texture => _webcam;

        /// <summary>Gets the selected device name, or an empty string when stopped.</summary>
        public string CurrentDeviceName => _currentDeviceName ?? string.Empty;

        /// <summary>Gets the event raised after the first camera frame is ready.</summary>
        public StatusEvent OnCameraStarted => onCameraStarted;

        /// <summary>Gets the event raised if permission, device selection, or startup fails.</summary>
        public StatusEvent OnError => onError;

        /// <summary>Gets whether a camera stream is currently playing.</summary>
        public bool IsPlaying => _webcam != null && _webcam.isPlaying;

        /// <summary>Sets the exact device name to select on the next call to <see cref="StartCamera"/>.</summary>
        public void SetPreferredDeviceName(string value) => preferredDeviceName = value?.Trim() ?? string.Empty;

        /// <summary>Sets whether device selection prefers a front-facing camera when no exact name is set.</summary>
        public void SetPreferFrontFacing(bool value) => preferFrontFacing = value;

        /// <summary>Sets whether the displayed feed is mirrored for front-facing cameras.</summary>
        public void SetMirrorFrontFacingCamera(bool value) => mirrorFrontFacingCamera = value;

        /// <summary>Sets the requested capture width. Takes effect on the next camera start.</summary>
        public void SetRequestedWidth(int value)
        {
            if (value < 16)
                throw new ArgumentOutOfRangeException(nameof(value), "Requested width must be at least 16 pixels.");
            requestedWidth = value;
        }

        /// <summary>Sets the requested capture height. Takes effect on the next camera start.</summary>
        public void SetRequestedHeight(int value)
        {
            if (value < 16)
                throw new ArgumentOutOfRangeException(nameof(value), "Requested height must be at least 16 pixels.");
            requestedHeight = value;
        }

        /// <summary>Sets the requested capture frame rate. Takes effect on the next camera start.</summary>
        public void SetRequestedFrameRate(int value)
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "Requested frame rate must be positive.");
            requestedFrameRate = value;
        }

        /// <summary>Sets the startup timeout in seconds.</summary>
        public void SetStartupTimeoutSeconds(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 1f)
                throw new ArgumentOutOfRangeException(nameof(value), "Startup timeout must be a finite value of at least one second.");
            startupTimeoutSeconds = value;
        }

        /// <summary>Gets the names of camera devices currently available to Unity.</summary>
        public static string[] GetDeviceNames()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            string[] names = new string[devices.Length];
            for (int i = 0; i < devices.Length; i++)
                names[i] = devices[i].name;
            return names;
        }

        private void OnEnable()
        {
            if (playOnEnable)
                StartCamera();
        }

        private void OnDisable() => StopCamera();

        /// <summary>Requests webcam permission, selects a device, and begins its stream.</summary>
        public void StartCamera()
        {
            StopCamera();
            _startupCoroutine = StartCoroutine(StartCameraRoutine());
        }

        /// <summary>Stops the stream and releases its texture.</summary>
        public void StopCamera()
        {
            if (_startupCoroutine != null)
            {
                StopCoroutine(_startupCoroutine);
                _startupCoroutine = null;
            }

            if (_webcam != null)
            {
                WebCamTexture previous = _webcam;
                previous.Stop();
                if (preview != null && preview.texture == previous)
                {
                    preview.texture = null;
                    preview.uvRect = _previewOriginalUvRect;
                    preview.rectTransform.localRotation = _previewOriginalRotation;
                }

                _webcam = null;
                _currentDeviceName = null;
                Destroy(previous);
            }
        }

        private IEnumerator StartCameraRoutine()
        {
#if UNITY_IOS || UNITY_WEBGL
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                RaiseError("Camera permission was not granted.");
                yield break;
            }
#endif

            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                RaiseError("No camera devices are available.");
                yield break;
            }

            int selectedIndex = FindDeviceIndex(devices);
            if (selectedIndex < 0)
            {
                RaiseError($"Camera device '{preferredDeviceName}' was not found.");
                yield break;
            }

            _webcam = new WebCamTexture(devices[selectedIndex].name, requestedWidth, requestedHeight, requestedFrameRate);
            _webcam.Play();

            float deadline = Time.realtimeSinceStartup + startupTimeoutSeconds;
            while (_webcam != null && !_webcam.didUpdateThisFrame && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (_webcam == null || !_webcam.didUpdateThisFrame)
            {
                ReleaseWebcam();
                RaiseError("The camera did not provide a frame before the startup timeout.");
                yield break;
            }

            if (preview != null)
                ApplyPreview(_webcam, devices[selectedIndex].isFrontFacing);

            string deviceName = devices[selectedIndex].name;
            _currentDeviceName = deviceName;
            _startupCoroutine = null;
            onCameraStarted?.Invoke(deviceName);
        }

        private int FindDeviceIndex(WebCamDevice[] devices)
        {
            if (!string.IsNullOrWhiteSpace(preferredDeviceName))
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    if (string.Equals(devices[i].name, preferredDeviceName, StringComparison.Ordinal))
                        return i;
                }

                return -1;
            }

            if (preferFrontFacing)
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    if (devices[i].isFrontFacing)
                        return i;
                }
            }

            return 0;
        }

        private void ApplyPreview(WebCamTexture webcam, bool isFrontFacing)
        {
            CapturePreviewOriginalState();
            preview.texture = webcam;
            preview.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -webcam.videoRotationAngle);

            bool mirrorHorizontally = mirrorFrontFacingCamera && isFrontFacing;
            bool mirrorVertically = webcam.videoVerticallyMirrored;
            preview.uvRect = new Rect(
                mirrorHorizontally ? 1f : 0f,
                mirrorVertically ? 1f : 0f,
                mirrorHorizontally ? -1f : 1f,
                mirrorVertically ? -1f : 1f);

            AspectRatioFitter fitter = preview.GetComponent<AspectRatioFitter>();
            if (fitter != null && webcam.height > 0)
            {
                bool rotated = webcam.videoRotationAngle == 90 || webcam.videoRotationAngle == 270;
                fitter.aspectRatio = rotated
                    ? (float)webcam.height / webcam.width
                    : (float)webcam.width / webcam.height;
            }
        }

        private void CapturePreviewOriginalState()
        {
            _previewOriginalRotation = preview.rectTransform.localRotation;
            _previewOriginalUvRect = preview.uvRect;
        }

        private void RaiseError(string message)
        {
            _startupCoroutine = null;
            Debug.LogWarning(message, this);
            onError?.Invoke(message);
        }

        private void ReleaseWebcam()
        {
            if (_webcam == null)
                return;

            WebCamTexture failed = _webcam;
            _webcam = null;
            failed.Stop();
            Destroy(failed);
        }
    }
}
