using System;

namespace PixelPlay.Foundation.Gestures
{
    /// <summary>The top gesture candidate reported for one hand in a frame.</summary>
    public readonly struct GestureCandidate
    {
        /// <summary>Creates the highest-ranked gesture candidate reported for one hand in a frame.</summary>
        /// <param name="handId">Stable ID assigned to the hand by the provider.</param>
        /// <param name="gestureId">Provider-defined gesture name or ID.</param>
        /// <param name="confidence">Recognition confidence in the inclusive range zero to one.</param>
        public GestureCandidate(string handId, string gestureId, float confidence)
        {
            if (string.IsNullOrWhiteSpace(handId))
                throw new ArgumentException("Hand ID must not be null or whitespace.", nameof(handId));
            if (string.IsNullOrWhiteSpace(gestureId))
                throw new ArgumentException("Gesture ID must not be null or whitespace.", nameof(gestureId));
            if (float.IsNaN(confidence) || float.IsInfinity(confidence) || confidence < 0f || confidence > 1f)
                throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between zero and one.");

            HandId = handId;
            GestureId = gestureId;
            Confidence = confidence;
        }

        /// <summary>Gets the provider-stable ID of the hand.</summary>
        public string HandId { get; }
        /// <summary>Gets the provider-defined ID of the recognized gesture candidate.</summary>
        public string GestureId { get; }
        /// <summary>Gets the confidence score in the inclusive range zero to one.</summary>
        public float Confidence { get; }
    }
}
