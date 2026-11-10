using System;
using PixelPlay.Foundation.Scoring;
using PixelPlay.Foundation.Timing;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Connects the platform-independent timer and score systems to Unity.</summary>
    public sealed class FoundationQuickStart : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float roundDurationSeconds = 15f;
        [SerializeField] private int pointsPerClick = 10;
        [SerializeField, Min(0)] private int fixedCompletionBonus = 50;
        [SerializeField, Min(0)] private int pointsPerElapsedSecond = 2;

        private CountdownTimer _timer;
        private ScoreCounter _score;
        private int _awardedElapsedSeconds;

        public event Action<float> TimerUpdated;
        public event Action<int> ScoreUpdated;
        public float RemainingSeconds => _timer != null ? _timer.RemainingSeconds : roundDurationSeconds;
        public int CurrentScore => _score != null ? _score.Value : 0;

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

        [ContextMenu("Add Points")]
        public void AddPoints() => _score.Add(pointsPerClick);

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
