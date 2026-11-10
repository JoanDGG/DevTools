using System;
using System.Collections.Generic;

namespace PixelPlay.Foundation.Gestures
{
    /// <summary>Filters gesture candidates with confidence hysteresis and time-based stability.</summary>
    public sealed class GestureRecognitionFilter
    {
        private readonly Dictionary<string, HandState> _states =
            new Dictionary<string, HandState>(StringComparer.Ordinal);

        public GestureRecognitionFilter(float startConfidence = 0.7f, float releaseConfidence = 0.45f,
            float activationSeconds = 0.12f, float releaseSeconds = 0.18f)
        {
            ValidateConfidence(startConfidence, nameof(startConfidence));
            ValidateConfidence(releaseConfidence, nameof(releaseConfidence));
            if (releaseConfidence > startConfidence)
                throw new ArgumentException("Release confidence must not exceed start confidence.");
            if (float.IsNaN(activationSeconds) || float.IsInfinity(activationSeconds) || activationSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(activationSeconds));
            if (float.IsNaN(releaseSeconds) || float.IsInfinity(releaseSeconds) || releaseSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(releaseSeconds));

            StartConfidence = startConfidence;
            ReleaseConfidence = releaseConfidence;
            ActivationSeconds = activationSeconds;
            ReleaseSeconds = releaseSeconds;
        }

        /// <summary>Gets the confidence required to begin recognizing a gesture.</summary>
        public float StartConfidence { get; }

        /// <summary>Gets the lower confidence at which an active gesture remains recognized.</summary>
        public float ReleaseConfidence { get; }

        /// <summary>Gets how long a candidate must remain stable before starting.</summary>
        public float ActivationSeconds { get; }

        /// <summary>Gets how long a candidate may be missing or weak before ending.</summary>
        public float ReleaseSeconds { get; }

        /// <summary>Raised when a filtered gesture starts or ends.</summary>
        public event Action<GestureStateChange> StateChanged;

        /// <summary>Processes one provider frame and raises any gesture state changes.</summary>
        public void Process(GestureFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            var candidates = new Dictionary<string, GestureCandidate>(StringComparer.Ordinal);
            for (int i = 0; i < frame.Candidates.Count; i++)
                candidates.Add(frame.Candidates[i].HandId, frame.Candidates[i]);

            var hands = new HashSet<string>(_states.Keys, StringComparer.Ordinal);
            foreach (string handId in candidates.Keys)
                hands.Add(handId);

            foreach (string handId in hands)
            {
                if (!_states.TryGetValue(handId, out HandState state))
                {
                    state = new HandState();
                    _states.Add(handId, state);
                }

                bool hasCandidate = candidates.TryGetValue(handId, out GestureCandidate candidate);
                ProcessHand(handId, state, hasCandidate, candidate, frame.TimestampSeconds);

                if (state.ActiveGestureId == null && state.CandidateGestureId == null)
                    _states.Remove(handId);
            }
        }

        /// <summary>Ends active gestures and clears temporal state, for example when a provider stops.</summary>
        public void Reset(double timestampSeconds)
        {
            if (double.IsNaN(timestampSeconds) || double.IsInfinity(timestampSeconds) || timestampSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(timestampSeconds));

            foreach (KeyValuePair<string, HandState> item in _states)
            {
                if (item.Value.ActiveGestureId != null)
                    RaiseChange(GestureStateChange.ChangeKind.Ended, item.Key, item.Value.ActiveGestureId,
                        item.Value.ActiveConfidence, timestampSeconds);
            }

            _states.Clear();
        }

        private void ProcessHand(string handId, HandState state, bool hasCandidate,
            GestureCandidate candidate, double timestampSeconds)
        {
            bool candidateCanStart = hasCandidate && candidate.Confidence >= StartConfidence;
            bool activeStillDetected = hasCandidate && candidate.GestureId == state.ActiveGestureId &&
                                       candidate.Confidence >= ReleaseConfidence;

            if (state.ActiveGestureId != null)
            {
                if (activeStillDetected)
                {
                    state.ActiveConfidence = candidate.Confidence;
                    state.LostSince = null;
                    ClearPendingCandidate(state);
                    return;
                }

                if (candidateCanStart && candidate.GestureId != state.ActiveGestureId)
                {
                    TrackPendingCandidate(state, candidate, timestampSeconds);
                    if (Elapsed(state.CandidateSince, timestampSeconds) >= ActivationSeconds)
                    {
                        EndActiveGesture(handId, state, timestampSeconds);
                        StartGesture(handId, state, candidate, timestampSeconds);
                        return;
                    }
                }
                else
                {
                    ClearPendingCandidate(state);
                }

                if (!state.LostSince.HasValue)
                    state.LostSince = timestampSeconds;

                if (timestampSeconds - state.LostSince.Value >= ReleaseSeconds)
                {
                    EndActiveGesture(handId, state, timestampSeconds);
                    if (candidateCanStart)
                    {
                        TrackPendingCandidate(state, candidate, timestampSeconds);
                        if (Elapsed(state.CandidateSince, timestampSeconds) >= ActivationSeconds)
                            StartGesture(handId, state, candidate, timestampSeconds);
                    }
                }

                return;
            }

            state.LostSince = null;
            if (!candidateCanStart)
            {
                ClearPendingCandidate(state);
                return;
            }

            TrackPendingCandidate(state, candidate, timestampSeconds);
            if (Elapsed(state.CandidateSince, timestampSeconds) >= ActivationSeconds)
                StartGesture(handId, state, candidate, timestampSeconds);
        }

        private void TrackPendingCandidate(HandState state, GestureCandidate candidate, double timestampSeconds)
        {
            if (state.CandidateGestureId == candidate.GestureId)
                return;

            state.CandidateGestureId = candidate.GestureId;
            state.CandidateSince = timestampSeconds;
        }

        private void StartGesture(string handId, HandState state, GestureCandidate candidate, double timestampSeconds)
        {
            state.ActiveGestureId = candidate.GestureId;
            state.ActiveConfidence = candidate.Confidence;
            state.LostSince = null;
            ClearPendingCandidate(state);
            RaiseChange(GestureStateChange.ChangeKind.Started, handId, candidate.GestureId,
                candidate.Confidence, timestampSeconds);
        }

        private void EndActiveGesture(string handId, HandState state, double timestampSeconds)
        {
            string gestureId = state.ActiveGestureId;
            float confidence = state.ActiveConfidence;
            state.ActiveGestureId = null;
            state.LostSince = null;
            RaiseChange(GestureStateChange.ChangeKind.Ended, handId, gestureId, confidence, timestampSeconds);
        }

        private void RaiseChange(GestureStateChange.ChangeKind kind, string handId, string gestureId,
            float confidence, double timestampSeconds)
        {
            StateChanged?.Invoke(new GestureStateChange(kind, handId, gestureId, confidence, timestampSeconds));
        }

        private static void ClearPendingCandidate(HandState state)
        {
            state.CandidateGestureId = null;
            state.CandidateSince = null;
        }

        private static double Elapsed(double? since, double now) => since.HasValue ? Math.Max(0d, now - since.Value) : 0d;

        private static void ValidateConfidence(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
                throw new ArgumentOutOfRangeException(parameterName, "Confidence must be between zero and one.");
        }

        private sealed class HandState
        {
            public string CandidateGestureId;
            public double? CandidateSince;
            public string ActiveGestureId;
            public float ActiveConfidence;
            public double? LostSince;
        }
    }
}
