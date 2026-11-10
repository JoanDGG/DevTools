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
        [Serializable]
        public sealed class StatusEvent : UnityEvent<string> { }

        [Header("Preview")]
        [SerializeField] private RawImage preview;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool mirrorFrontFacingCamera = true;

        [Header("Device")]
        [SerializeField] private string preferredDeviceName;
        [SerializeField] private bool preferFrontFacing;
        [SerializeField, Min(16)] private int requestedWidth = 1280;
        [SerializeField, Min(16)] private int requestedHeight = 720;
        [SerializeField, Min(1)] private int requestedFrameRate = 30;
        [SerializeField, Min(1f)] private float startupTimeoutSeconds = 10f;

        [Header("Events")]
        [SerializeField] private StatusEvent onCameraStarted = new StatusEvent();
        [SerializeField] private StatusEvent onError = new StatusEvent();

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
