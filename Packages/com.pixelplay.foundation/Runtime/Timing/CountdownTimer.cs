using System;

namespace PixelPlay.Foundation.Timing
{
    /// <summary>A countdown timer advanced explicitly by its owner.</summary>
    /// <remarks>
    /// This class does not depend on Unity's frame loop. Call <see cref="Advance"/>
    /// with the desired time step, such as Time.deltaTime or Time.unscaledDeltaTime.
    /// </remarks>
    public sealed class CountdownTimer : IResettable
    {
        private float _initialDuration;

        /// <summary>Raised after each advance while the timer is running.</summary>
        public event Action<float> Ticked;

        /// <summary>Raised once when the countdown reaches zero.</summary>
        public event Action Completed;

        /// <summary>Creates a timer with the specified duration in seconds.</summary>
        public CountdownTimer(float durationSeconds)
        {
            SetDuration(durationSeconds);
            RemainingSeconds = _initialDuration;
        }

        /// <summary>Gets the configured duration in seconds.</summary>
        public float DurationSeconds => _initialDuration;

        /// <summary>Gets the seconds remaining.</summary>
        public float RemainingSeconds { get; private set; }

        /// <summary>Gets whether the timer is currently counting down.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Gets whether the countdown has reached zero.</summary>
        public bool IsCompleted => RemainingSeconds <= 0f;

        /// <summary>Sets the duration and resets the remaining time.</summary>
        public void SetDuration(float durationSeconds)
        {
            if (float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds))
                throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Duration must be a finite number.");

            _initialDuration = Math.Max(0f, durationSeconds);
            RemainingSeconds = _initialDuration;
            IsRunning = false;
        }

        /// <summary>Starts or restarts the countdown from its configured duration.</summary>
        public void Start()
        {
            RemainingSeconds = _initialDuration;
            IsRunning = RemainingSeconds > 0f;
            if (!IsRunning)
                Completed?.Invoke();
        }

        /// <summary>Pauses the countdown without changing its remaining time.</summary>
        public void Pause() => IsRunning = false;

        /// <summary>Continues a paused countdown if time remains.</summary>
        public void Resume() => IsRunning = RemainingSeconds > 0f;

        /// <summary>Advances the countdown by a non-negative time step.</summary>
        public void Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta time must be finite and non-negative.");

            if (!IsRunning)
                return;

            RemainingSeconds = Math.Max(0f, RemainingSeconds - deltaSeconds);
            Ticked?.Invoke(RemainingSeconds);

            if (RemainingSeconds <= 0f)
            {
                IsRunning = false;
                Completed?.Invoke();
            }
        }

        /// <summary>Stops the countdown and restores its configured duration.</summary>
        public void Reset()
        {
            RemainingSeconds = _initialDuration;
            IsRunning = false;
        }
    }
}
