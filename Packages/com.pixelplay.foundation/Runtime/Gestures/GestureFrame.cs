using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PixelPlay.Foundation.Gestures
{
    /// <summary>An immutable set of top gesture candidates for a tracking frame.</summary>
    /// <remarks>Supply at most one candidate per hand, usually the provider's highest-confidence result.</remarks>
    public sealed class GestureFrame
    {
        private readonly ReadOnlyCollection<GestureCandidate> _candidates;

        public GestureFrame(long frameId, double timestampSeconds, IEnumerable<GestureCandidate> candidates)
        {
            if (double.IsNaN(timestampSeconds) || double.IsInfinity(timestampSeconds) || timestampSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(timestampSeconds), "Timestamp must be finite and non-negative.");

            var copy = candidates == null ? new List<GestureCandidate>() : new List<GestureCandidate>(candidates);
            var hands = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < copy.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(copy[i].HandId) || string.IsNullOrWhiteSpace(copy[i].GestureId))
                    throw new ArgumentException("Candidate collection contains an uninitialized value.", nameof(candidates));
                if (!hands.Add(copy[i].HandId))
                    throw new ArgumentException($"More than one candidate was supplied for hand '{copy[i].HandId}'.", nameof(candidates));
            }

            FrameId = frameId;
            TimestampSeconds = timestampSeconds;
            _candidates = copy.AsReadOnly();
        }

        public long FrameId { get; }
        public double TimestampSeconds { get; }
        public IReadOnlyList<GestureCandidate> Candidates => _candidates;
    }
}
