using System;
using PixelPlay.Foundation.QrCodes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PixelPlay.Foundation.Unity.QrCodes
{
    /// <summary>Generates a QR texture and optionally assigns it to a UI RawImage.</summary>
    public sealed class QrCodeTextureGenerator : MonoBehaviour
    {
        [SerializeField, Tooltip("Optional RawImage that displays each generated QR texture.")] private RawImage target;
        [SerializeField, TextArea, Tooltip("Content encoded by GenerateConfiguredValue; short text or URLs are recommended.")] private string content = "https://example.com";
        [SerializeField, Min(1), Tooltip("Texture pixels used for each QR module. Higher values create a larger image.")] private int pixelsPerModule = 8;
        [SerializeField, Min(4), Tooltip("White border around the code in modules. Keep at least four for scanner reliability.")] private int quietZoneModules = 4;
        [SerializeField, Tooltip("Color used for dark QR modules.")] private Color32 darkColor = new Color32(0, 0, 0, 255);
        [SerializeField, Tooltip("Background and quiet-zone color; use an opaque light color for reliable scanning.")] private Color32 lightColor = new Color32(255, 255, 255, 255);
        [SerializeField, Tooltip("Invoked when content cannot be encoded or the texture cannot be created.")] private UnityEvent<string> onError = new UnityEvent<string>();

        private readonly QrCodeEncoder _encoder = new QrCodeEncoder();
        private Texture2D _generatedTexture;

        /// <summary>Gets the current generated texture.</summary>
        public Texture2D GeneratedTexture => _generatedTexture;

        /// <summary>Gets the event raised when content cannot be encoded or rendered.</summary>
        public UnityEvent<string> OnError => onError;

        /// <summary>Sets the content used by <see cref="GenerateConfiguredValue"/>.</summary>
        public void SetContent(string value) => content = value ?? string.Empty;

        /// <summary>Sets texture resolution for subsequent generation calls.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Value is less than one pixel per module.</exception>
        public void SetPixelsPerModule(int value)
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "Pixels per module must be at least one.");
            pixelsPerModule = value;
        }

        /// <summary>Sets the QR quiet-zone border for subsequent generation calls.</summary>
        /// <exception cref="ArgumentOutOfRangeException">Value is less than the required four modules.</exception>
        public void SetQuietZoneModules(int value)
        {
            if (value < 4)
                throw new ArgumentOutOfRangeException(nameof(value), "Quiet zone must be at least four modules.");
            quietZoneModules = value;
        }

        /// <summary>Encodes text and creates a point-filtered texture with a four-module quiet zone.</summary>
        /// <param name="value">Text or URL to encode.</param>
        /// <returns>The generated QR texture.</returns>
        /// <exception cref="ArgumentException">Content is empty, invalid Unicode, or exceeds encoder capacity.</exception>
        public Texture2D GenerateTexture(string value)
        {
            try
            {
                QrCodeMatrix matrix = _encoder.EncodeText(value);
                Texture2D nextTexture = CreateTexture(matrix, pixelsPerModule, quietZoneModules, darkColor, lightColor);

                Texture2D previousTexture = _generatedTexture;
                _generatedTexture = nextTexture;
                if (target != null)
                {
                    target.texture = nextTexture;
                    target.color = Color.white;
                }

                if (previousTexture != null)
                    Destroy(previousTexture);
                return nextTexture;
            }
            catch (Exception exception)
            {
                onError?.Invoke(exception.Message);
                throw;
            }
        }

        /// <summary>Inspector-friendly action that generates the serialized content value.</summary>
        public void GenerateConfiguredValue()
        {
            try
            {
                GenerateTexture(content);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not generate QR code: {exception.Message}", this);
            }
        }

        /// <summary>Creates a square QR texture from an encoded matrix.</summary>
        /// <param name="matrix">Encoded module matrix.</param>
        /// <param name="pixelsPerModule">Pixel size for each module.</param>
        /// <param name="quietZoneModules">White border width in modules; must be at least four.</param>
        /// <param name="dark">Color for dark modules.</param>
        /// <param name="light">Background and quiet-zone color.</param>
        /// <returns>Point-filtered texture; caller owns its lifetime.</returns>
        public static Texture2D CreateTexture(QrCodeMatrix matrix, int pixelsPerModule, int quietZoneModules,
            Color32 dark, Color32 light)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));
            if (pixelsPerModule < 1)
                throw new ArgumentOutOfRangeException(nameof(pixelsPerModule));
            if (quietZoneModules < 4)
                throw new ArgumentOutOfRangeException(nameof(quietZoneModules), "QR codes require at least four quiet-zone modules.");

            long totalModules = matrix.ModuleCount + 2L * quietZoneModules;
            long dimension = totalModules * pixelsPerModule;
            if (dimension > 2048)
                throw new ArgumentOutOfRangeException(nameof(pixelsPerModule), "Generated QR texture dimensions must not exceed 2048 pixels.");

            int pixelDimension = (int)dimension;
            int moduleDimension = (int)totalModules;
            var pixels = new Color32[pixelDimension * pixelDimension];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = light;

            for (int moduleYFromBottom = 0; moduleYFromBottom < moduleDimension; moduleYFromBottom++)
            {
                int matrixY = moduleDimension - 1 - moduleYFromBottom - quietZoneModules;
                if (matrixY < 0 || matrixY >= matrix.ModuleCount)
                    continue;

                for (int moduleXWithQuietZone = 0; moduleXWithQuietZone < moduleDimension; moduleXWithQuietZone++)
                {
                    int matrixX = moduleXWithQuietZone - quietZoneModules;
                    if (matrixX < 0 || matrixX >= matrix.ModuleCount || !matrix.IsDark(matrixX, matrixY))
                        continue;

                    int pixelX = moduleXWithQuietZone * pixelsPerModule;
                    int pixelY = moduleYFromBottom * pixelsPerModule;
                    for (int dy = 0; dy < pixelsPerModule; dy++)
                    {
                        int pixelRow = (pixelY + dy) * pixelDimension + pixelX;
                        for (int dx = 0; dx < pixelsPerModule; dx++)
                            pixels[pixelRow + dx] = dark;
                    }
                }
            }

            var texture = new Texture2D(pixelDimension, pixelDimension, TextureFormat.RGBA32, false);
            try
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                return texture;
            }
            catch
            {
                Destroy(texture);
                throw;
            }
        }

        private void OnDestroy()
        {
            if (target != null && target.texture == _generatedTexture)
                target.texture = null;
            if (_generatedTexture != null)
                Destroy(_generatedTexture);
            _generatedTexture = null;
        }
    }
}
