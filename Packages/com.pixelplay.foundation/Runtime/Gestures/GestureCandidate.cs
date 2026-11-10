using System;

namespace PixelPlay.Foundation.Gestures
{
    /// <summary>The top gesture candidate reported for one hand in a frame.</summary>
    public readonly struct GestureCandidate
    {
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

        public string HandId { get; }
        public string GestureId { get; }
        public float Confidence { get; }
    }
}
