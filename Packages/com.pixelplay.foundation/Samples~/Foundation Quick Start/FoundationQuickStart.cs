using System;
using PixelPlay.Foundation.Scoring;
using PixelPlay.Foundation.Timing;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Connects the platform-independent timer and score systems to Unity.</summary>
    public sealed class FoundationQuickStart : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Countdown duration for this demo round, in seconds.")] private float roundDurationSeconds = 15f;
        [SerializeField, Tooltip("Points added whenever the demo's Add Points action is invoked.")] private int pointsPerClick = 10;
        [SerializeField, Min(0), Tooltip("One-time bonus awarded when the countdown completes.")] private int fixedCompletionBonus = 50;
        [SerializeField, Min(0), Tooltip("Points awarded once for each whole second elapsed during the round.")] private int pointsPerElapsedSecond = 2;

        private CountdownTimer _timer;
        private ScoreCounter _score;
        private int _awardedElapsedSeconds;

        /// <summary>Raised when the countdown value changes, with seconds remaining.</summary>
        public event Action<float> TimerUpdated;
        /// <summary>Raised when the score changes, with the new total.</summary>
        public event Action<int> ScoreUpdated;
        /// <summary>Gets the remaining time, or the configured duration before initialization.</summary>
        public float RemainingSeconds => _timer != null ? _timer.RemainingSeconds : roundDurationSeconds;
        /// <summary>Gets the current score, or zero before initialization.</summary>
        public int CurrentScore => _score != null ? _score.Value : 0;

        /// <summary>Gets the configured round duration in seconds.</summary>
        public float RoundDurationSeconds => roundDurationSeconds;

        /// <summary>Sets the round duration. An active timer restarts from the new full duration.</summary>
        public void SetRoundDurationSeconds(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(nameof(value), "Round duration must be finite and non-negative.");

            bool restartActiveTimer = _timer != null && _timer.IsRunning;
            roundDurationSeconds = value;
            _awardedElapsedSeconds = 0;
            if (_timer == null)
                return;

            _timer.SetDuration(value);
            if (restartActiveTimer)
                _timer.Start();
            TimerUpdated?.Invoke(_timer.RemainingSeconds);
        }

        /// <summary>Sets points added by <see cref="AddPoints"/>. Negative values can be used as penalties.</summary>
        public void SetPointsPerClick(int value) => pointsPerClick = value;

        /// <summary>Sets the one-time score bonus awarded when the timer completes.</summary>
        public void SetFixedCompletionBonus(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Completion bonus must be non-negative.");
            fixedCompletionBonus = value;
        }

        /// <summary>Sets points awarded for each whole elapsed second from this point onward.</summary>
        public void SetPointsPerElapsedSecond(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Elapsed-second points must be non-negative.");
            pointsPerElapsedSecond = value;
        }

        private void Awake()
        {
            _timer = new CountdownTimer(roundDurationSeconds);
            _score = new ScoreCounter();
            _timer.Completed += OnTimerCompleted;
            _timer.Ticked += OnTimerTicked;
            _score.Changed += OnScoreChanged;
        }

        private void Start() => _timer.Start();
        private void Update() => _timer.Advance(Time.deltaTime);

        /// <summary>Adds the configured click points to the score.</summary>
        [ContextMenu("Add Points")]
        public void AddPoints() => _score.Add(pointsPerClick);

        /// <summary>Resets the score and starts a new countdown.</summary>
        [ContextMenu("Reset Demo")]
        public void ResetDemo()
        {
            _timer.Reset();
            _score.Reset();
            _awardedElapsedSeconds = 0;
            _timer.Start();
            TimerUpdated?.Invoke(_timer.RemainingSeconds);
        }

        private void OnTimerCompleted()
        {
            if (fixedCompletionBonus > 0)
                _score.Add(fixedCompletionBonus);
        }

        private void OnTimerTicked(float remainingSeconds)
        {
            TimerUpdated?.Invoke(remainingSeconds);
            int elapsedSeconds = Mathf.FloorToInt(Mathf.Max(0f, _timer.DurationSeconds - remainingSeconds));
            int secondsToAward = elapsedSeconds - _awardedElapsedSeconds;
            _awardedElapsedSeconds = elapsedSeconds;
            if (secondsToAward > 0 && pointsPerElapsedSecond > 0)
                _score.Add(checked(secondsToAward * pointsPerElapsedSecond));
        }

        private void OnScoreChanged(int previous, int current) => ScoreUpdated?.Invoke(current);

        private void OnDestroy()
        {
            if (_timer != null)
            {
                _timer.Completed -= OnTimerCompleted;
                _timer.Ticked -= OnTimerTicked;
            }
            if (_score != null)
                _score.Changed -= OnScoreChanged;
        }
    }
}
