using System;

namespace PixelPlay.Foundation.Tracking
{
    /// <summary>A named point in a tracked entity, expressed in normalized image coordinates.</summary>
    /// <remarks>X and Y use a top-left image origin. Depth units are provider-defined.</remarks>
    public readonly struct TrackedLandmark
    {
        /// <summary>Creates a landmark in normalized source-image coordinates.</summary>
        /// <param name="id">Provider-stable landmark ID such as <c>left_wrist</c>.</param>
        /// <param name="normalizedX">Horizontal coordinate; zero is the left edge and one is the right edge.</param>
        /// <param name="normalizedY">Vertical coordinate; zero is the top edge and one is the bottom edge.</param>
        /// <param name="depth">Depth value in units defined by the provider.</param>
        /// <param name="confidence">Confidence in the inclusive range zero to one.</param>
        public TrackedLandmark(string id, float normalizedX, float normalizedY, float depth, float confidence)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Landmark ID must not be null or whitespace.", nameof(id));
            if (float.IsNaN(normalizedX) || float.IsInfinity(normalizedX) ||
                float.IsNaN(normalizedY) || float.IsInfinity(normalizedY) ||
                float.IsNaN(depth) || float.IsInfinity(depth))
                throw new ArgumentOutOfRangeException(nameof(normalizedX), "Landmark coordinates must be finite numbers.");
            if (float.IsNaN(confidence) || float.IsInfinity(confidence) || confidence < 0f || confidence > 1f)
                throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between zero and one.");

            Id = id;
            NormalizedX = normalizedX;
            NormalizedY = normalizedY;
            Depth = depth;
            Confidence = confidence;
        }

        /// <summary>Gets the provider's stable landmark name, such as `left_wrist`.</summary>
        public string Id { get; }

        /// <summary>Gets the normalized horizontal coordinate; values outside zero to one are allowed.</summary>
        public float NormalizedX { get; }

        /// <summary>Gets the normalized vertical coordinate, measured from the top of the image.</summary>
        public float NormalizedY { get; }

        /// <summary>Gets the provider-defined depth value.</summary>
        public float Depth { get; }

        /// <summary>Gets confidence normalized to zero through one.</summary>
        public float Confidence { get; }
    }
}
