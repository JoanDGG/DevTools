using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace PixelPlay.Foundation.Unity.Camera
{
    /// <summary>Encodes a webcam frame as PNG bytes and optionally saves it locally.</summary>
    public sealed class WebcamSnapshotCapture : MonoBehaviour
    {
        [Serializable]
        public sealed class PathEvent : UnityEvent<string> { }

        [SerializeField] private WebcamPreview cameraSource;
        [SerializeField] private string fileNamePrefix = "webcam_capture";
        [SerializeField] private PathEvent onImageSaved = new PathEvent();
        [SerializeField] private PathEvent onError = new PathEvent();

        /// <summary>Gets the event raised with the full path after an image is saved.</summary>
        public PathEvent OnImageSaved => onImageSaved;

        /// <summary>Gets the event raised when the capture-and-save action fails.</summary>
        public PathEvent OnError => onError;

        /// <summary>Encodes the latest active webcam frame as PNG bytes.</summary>
        /// <exception cref="InvalidOperationException">The camera is not streaming a valid frame.</exception>
        public byte[] CapturePngBytes()
        {
            if (cameraSource == null || cameraSource.Texture == null || !cameraSource.IsPlaying)
                throw new InvalidOperationException("A running WebcamPreview is required to capture an image.");

            WebCamTexture webcam = cameraSource.Texture;
            if (webcam.width < 16 || webcam.height < 16)
                throw new InvalidOperationException("The webcam has not produced a valid image yet.");

            Color32[] pixels = webcam.GetPixels32();
            Texture2D image = new Texture2D(webcam.width, webcam.height, TextureFormat.RGBA32, false);
            try
            {
                image.SetPixels32(pixels);
                image.Apply(false, false);
                return image.EncodeToPNG();
            }
            finally
            {
                Destroy(image);
            }
        }

        /// <summary>Saves a PNG under persistentDataPath and returns its full path.</summary>
        public string SavePngToPersistentDataPath(string fileName = null)
        {
            byte[] pngBytes = CapturePngBytes();
            string directory = Application.persistentDataPath;
            Directory.CreateDirectory(directory);

            string baseName = CreateSafeBaseName(fileName);
            string path = Path.Combine(directory, baseName + ".png");
            int suffix = 1;
            while (File.Exists(path))
            {
                path = Path.Combine(directory, baseName + "_" + suffix + ".png");
                suffix++;
            }

            File.WriteAllBytes(path, pngBytes);
            onImageSaved?.Invoke(path);
            return path;
        }

        /// <summary>Inspector-friendly action that captures and saves a uniquely named PNG.</summary>
        public void CaptureAndSave()
        {
            try
            {
                SavePngToPersistentDataPath();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not capture webcam image: {exception.Message}", this);
                onError?.Invoke(exception.Message);
            }
        }

        private string CreateSafeBaseName(string requestedName)
        {
            string baseName = string.IsNullOrWhiteSpace(requestedName)
                ? $"{fileNamePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}"
                : Path.GetFileNameWithoutExtension(requestedName.Replace('\\', '/'));

            foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(invalidCharacter, '_');

            if (string.IsNullOrWhiteSpace(baseName) || baseName == "." || baseName == "..")
                baseName = $"webcam_capture_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}";

            return baseName;
        }
    }
}
