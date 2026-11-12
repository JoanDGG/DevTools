using PixelPlay.Foundation.Unity.QrCodes;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Generates a sample QR code when the scene starts.</summary>
    public sealed class QrCodeQuickStart : MonoBehaviour
    {
        [SerializeField, Tooltip("Generator that creates and displays the sample QR texture.")] private QrCodeTextureGenerator generator;
        [SerializeField, TextArea, Tooltip("Short text or URL encoded when the scene starts.")] private string sampleContent = "https://example.com/quick-hide";

        /// <summary>Gets the content currently configured for the sample.</summary>
        public string SampleContent => sampleContent;

        /// <summary>Updates the sample content and immediately refreshes the QR texture.</summary>
        public void SetSampleContent(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new System.ArgumentException("QR sample content must not be empty.", nameof(value));

            sampleContent = value;
            if (generator == null)
                generator = GetComponent<QrCodeTextureGenerator>();
            if (generator == null)
                throw new System.InvalidOperationException("Assign a QrCodeTextureGenerator before setting sample content.");
            generator.GenerateTexture(sampleContent);
        }

        private void Start()
        {
            if (generator == null)
                generator = GetComponent<QrCodeTextureGenerator>();
            if (generator == null)
            {
                Debug.LogError("Assign a QrCodeTextureGenerator to QrCodeQuickStart.", this);
                return;
            }

            generator.GenerateTexture(sampleContent);
        }
    }
}
