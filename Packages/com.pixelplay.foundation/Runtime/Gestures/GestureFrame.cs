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

        /// <summary>Creates an immutable frame with at most one candidate for each hand.</summary>
        /// <param name="frameId">Provider frame number.</param>
        /// <param name="timestampSeconds">Monotonic timestamp in seconds.</param>
        /// <param name="candidates">Candidate snapshot; null is treated as an empty collection.</param>
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

        /// <summary>Gets the provider frame number.</summary>
        public long FrameId { get; }
        /// <summary>Gets the monotonic timestamp in seconds.</summary>
        public double TimestampSeconds { get; }
        /// <summary>Gets the read-only candidate snapshot.</summary>
        public IReadOnlyList<GestureCandidate> Candidates => _candidates;
    }
}
