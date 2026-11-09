using System;
using PixelPlay.Foundation.Scoring;
using PixelPlay.Foundation.Timing;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>
    /// Demonstrates connecting the platform-independent timer and score systems
    /// to a Unity scene. Add this component to a GameObject and enter Play mode.
    /// </summary>
    public sealed class FoundationQuickStart : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float roundDurationSeconds = 15f;
        [SerializeField] private int pointsPerClick = 10;
        [SerializeField, Min(0)] private int fixedCompletionBonus = 50;
        [SerializeField, Min(0)] private int pointsPerElapsedSecond = 2;

        private CountdownTimer _timer;
        private ScoreCounter _score;
        private int _awardedElapsedSeconds;

        /// <summary>Raised when the displayed remaining time changes.</summary>
        public event Action<float> TimerUpdated;

        /// <summary>Raised when the score changes.</summary>
        public event Action<int> ScoreUpdated;

        /// <summary>Gets the current remaining time, in seconds.</summary>
        public float RemainingSeconds => _timer != null ? _timer.RemainingSeconds : roundDurationSeconds;

        /// <summary>Gets the current score.</summary>
        public int CurrentScore => _score != null ? _score.Value : 0;

        private void Awake()
        {
            _timer = new CountdownTimer(roundDurationSeconds);
            _score = new ScoreCounter();

            _timer.Completed += OnTimerCompleted;
            _timer.Ticked += OnTimerTicked;
            _score.Changed += OnScoreChanged;
        }

        private void Start()
        {
            _timer.Start();
        }

        private void Update()
        {
            _timer.Advance(Time.deltaTime);
        }

        /// <summary>Adds the configured points. Also available in the component context menu.</summary>
        [ContextMenu("Add Points")]
        public void AddPoints()
        {
            _score.Add(pointsPerClick);
        }

        /// <summary>Restarts the timer and restores the starting score.</summary>
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

            Debug.Log($"Foundation Quick Start: timer completed. Fixed bonus awarded: {fixedCompletionBonus}.", this);
        }

        private void OnTimerTicked(float remainingSeconds)
        {
            TimerUpdated?.Invoke(remainingSeconds);

            int elapsedWholeSeconds = Mathf.FloorToInt(
                Mathf.Max(0f, _timer.DurationSeconds - remainingSeconds));
            int secondsToAward = elapsedWholeSeconds - _awardedElapsedSeconds;
            _awardedElapsedSeconds = elapsedWholeSeconds;

            if (secondsToAward > 0 && pointsPerElapsedSecond > 0)
                _score.Add(checked(secondsToAward * pointsPerElapsedSecond));
        }

        private void OnScoreChanged(int previous, int current)
        {
            Debug.Log($"Foundation Quick Start: score {previous} -> {current}.", this);
            ScoreUpdated?.Invoke(current);
        }

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
