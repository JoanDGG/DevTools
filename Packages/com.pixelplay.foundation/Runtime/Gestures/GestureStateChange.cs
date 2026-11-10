namespace PixelPlay.Foundation.Gestures
{
    /// <summary>Describes a gesture starting or ending after confidence filtering.</summary>
    public readonly struct GestureStateChange
    {
        public enum ChangeKind
        {
            Started,
            Ended
        }

        public GestureStateChange(ChangeKind kind, string handId, string gestureId, float confidence, double timestampSeconds)
        {
            Kind = kind;
            HandId = handId;
            GestureId = gestureId;
            Confidence = confidence;
            TimestampSeconds = timestampSeconds;
        }

        public ChangeKind Kind { get; }
        public string HandId { get; }
        public string GestureId { get; }
        public float Confidence { get; }
        public double TimestampSeconds { get; }
    }
}
