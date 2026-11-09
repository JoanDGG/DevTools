using TMPro;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Displays a Foundation Quick Start timer and score using TextMeshPro UI fields.</summary>
    public sealed class FoundationQuickStartHUD : MonoBehaviour
    {
        [SerializeField] private FoundationQuickStart source;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private string timerFormat = "Time: {0:00}";
        [SerializeField] private string scoreFormat = "Score: {0}";

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
