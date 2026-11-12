namespace PixelPlay.Foundation.Gestures
{
    /// <summary>Describes a gesture starting or ending after confidence filtering.</summary>
    public readonly struct GestureStateChange
    {
        /// <summary>Kind of filtered gesture lifecycle change.</summary>
        public enum ChangeKind
        {
            /// <summary>A gesture became active after meeting the stability threshold.</summary>
            Started,
            /// <summary>A gesture stopped meeting the release conditions.</summary>
            Ended
        }

        /// <summary>Creates a gesture lifecycle change event.</summary>
        public GestureStateChange(ChangeKind kind, string handId, string gestureId, float confidence, double timestampSeconds)
        {
            Kind = kind;
            HandId = handId;
            GestureId = gestureId;
            Confidence = confidence;
            TimestampSeconds = timestampSeconds;
        }

        /// <summary>Gets whether the gesture started or ended.</summary>
        public ChangeKind Kind { get; }
        /// <summary>Gets the stable ID of the hand that changed.</summary>
        public string HandId { get; }
        /// <summary>Gets the provider-defined gesture ID.</summary>
        public string GestureId { get; }
        /// <summary>Gets the confidence at the time of the change.</summary>
        public float Confidence { get; }
        /// <summary>Gets the monotonic timestamp of the change, in seconds.</summary>
        public double TimestampSeconds { get; }
    }
}
