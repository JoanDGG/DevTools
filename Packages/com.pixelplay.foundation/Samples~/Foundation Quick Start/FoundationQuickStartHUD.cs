using TMPro;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Displays the Foundation Quick Start timer and score with TextMeshPro.</summary>
    public sealed class FoundationQuickStartHUD : MonoBehaviour
    {
        [SerializeField, Tooltip("Game component that provides timer and score values.")] private FoundationQuickStart source;
        [SerializeField, Tooltip("TextMeshPro label updated with the remaining time.")] private TextMeshProUGUI timerText;
        [SerializeField, Tooltip("TextMeshPro label updated with the current score.")] private TextMeshProUGUI scoreText;
        [SerializeField, Tooltip("Composite format string for the timer; argument zero is remaining seconds.")] private string timerFormat = "Time: {0:00}";
        [SerializeField, Tooltip("Composite format string for the score; argument zero is the score value.")] private string scoreFormat = "Score: {0}";

        /// <summary>Sets the timer label format; argument zero is remaining seconds.</summary>
        public void SetTimerFormat(string value)
        {
            timerFormat = value ?? string.Empty;
            if (source != null)
                UpdateTimer(source.RemainingSeconds);
        }

        /// <summary>Sets the score label format; argument zero is the current score.</summary>
        public void SetScoreFormat(string value)
        {
            scoreFormat = value ?? string.Empty;
            if (source != null)
                UpdateScore(source.CurrentScore);
        }

        private void OnEnable()
        {
            if (source == null)
                source = GetComponent<FoundationQuickStart>();
            if (source == null)
            {
                Debug.LogError("FoundationQuickStartHUD requires a FoundationQuickStart source.", this);
                return;
            }

            source.TimerUpdated += UpdateTimer;
            source.ScoreUpdated += UpdateScore;
            UpdateTimer(source.RemainingSeconds);
            UpdateScore(source.CurrentScore);
        }

        private void OnDisable()
        {
            if (source == null)
                return;
            source.TimerUpdated -= UpdateTimer;
            source.ScoreUpdated -= UpdateScore;
        }

        private void UpdateTimer(float remainingSeconds)
        {
            if (timerText != null)
                timerText.text = string.Format(timerFormat, Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds)));
        }

        private void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = string.Format(scoreFormat, score);
        }
    }
}
