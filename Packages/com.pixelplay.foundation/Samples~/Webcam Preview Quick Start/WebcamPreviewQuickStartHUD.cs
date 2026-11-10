using System;
using System.IO;
using PixelPlay.Foundation.Unity.Camera;
using PixelPlay.Foundation.Unity.Networking;
using TMPro;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Shows camera, snapshot, and image upload status in TextMeshPro.</summary>
    public sealed class WebcamPreviewQuickStartHUD : MonoBehaviour
    {
        [SerializeField] private WebcamPreview webcamPreview;
        [SerializeField] private WebcamSnapshotCapture snapshotCapture;
        [SerializeField] private MultipartImageUploader imageUploader;
        [SerializeField] private TextMeshProUGUI statusLabel;

        private void OnEnable()
        {
            if (webcamPreview == null)
                webcamPreview = GetComponent<WebcamPreview>();
            if (webcamPreview != null)
            {
                webcamPreview.OnCameraStarted.AddListener(HandleCameraStarted);
                webcamPreview.OnError.AddListener(HandleError);
            }
            if (snapshotCapture != null)
            {
                snapshotCapture.OnImageSaved.AddListener(HandleImageSaved);
                snapshotCapture.OnError.AddListener(HandleError);
            }
            if (imageUploader != null)
                imageUploader.OnError.AddListener(HandleError);
        }

        private void OnDisable()
        {
            if (webcamPreview != null)
            {
                webcamPreview.OnCameraStarted.RemoveListener(HandleCameraStarted);
                webcamPreview.OnError.RemoveListener(HandleError);
            }
            if (snapshotCapture != null)
            {
                snapshotCapture.OnImageSaved.RemoveListener(HandleImageSaved);
                snapshotCapture.OnError.RemoveListener(HandleError);
            }
            if (imageUploader != null)
                imageUploader.OnError.RemoveListener(HandleError);
        }

        public async void CaptureAndUpload()
        {
            if (snapshotCapture == null || imageUploader == null)
            {
                SetStatus("Assign the snapshot capture and uploader components.");
                return;
            }

            try
            {
                var response = await imageUploader.UploadPngAsync(snapshotCapture.CapturePngBytes());
                SetStatus(response.IsSuccessStatusCode ? $"Upload succeeded ({response.StatusCode})." : $"HTTP {response.StatusCode}");
            }
            catch (Exception exception)
            {
                SetStatus($"Upload error: {exception.Message}");
            }
        }

        private void HandleCameraStarted(string deviceName) => SetStatus($"Camera ready: {deviceName}");
        private void HandleImageSaved(string path) => SetStatus($"Saved: {Path.GetFileName(path)}");
        private void HandleError(string message) => SetStatus(message);

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }
    }
}
