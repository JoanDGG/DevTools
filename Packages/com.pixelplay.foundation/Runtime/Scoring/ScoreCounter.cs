using System;

namespace PixelPlay.Foundation.Scoring
{
    /// <summary>A small score value object that reports changes.</summary>
    public sealed class ScoreCounter : IResettable
    {
        private readonly int _startingScore;

        /// <summary>Raised with the previous and new score whenever the value changes.</summary>
        public event Action<int, int> Changed;

        /// <summary>Creates a counter with an optional starting score.</summary>
        public ScoreCounter(int startingScore = 0)
        {
            _startingScore = startingScore;
            Value = startingScore;
        }

        /// <summary>Gets the current score.</summary>
        public int Value { get; private set; }

        /// <summary>Sets the score to an absolute value.</summary>
        public void SetScore(int score) => SetValue(score);

        /// <summary>Adds points to the current score.</summary>
        public void Add(int points) => SetValue(checked(Value + points));

        /// <summary>Subtracts points from the current score.</summary>
        public void Subtract(int points) => SetValue(checked(Value - points));

        /// <summary>Restores the starting score.</summary>
        public void Reset() => SetValue(_startingScore);

        private void SetValue(int value)
        {
            if (value == Value)
                return;

            int previousValue = Value;
            Value = value;
            Changed?.Invoke(previousValue, Value);
        }
    }
}
