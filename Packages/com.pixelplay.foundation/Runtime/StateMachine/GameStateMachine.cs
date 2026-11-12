using System;

namespace PixelPlay.Foundation.StateMachine
{
    /// <summary>Runs one active game state and coordinates state lifecycle transitions.</summary>
    public sealed class GameStateMachine
    {
        private bool _isTransitioning;

        /// <summary>Raised after a transition has completed, with the previous and current states.</summary>
        public event Action<IGameState, IGameState> StateChanged;

        /// <summary>Gets the active state, or null when the machine is stopped.</summary>
        public IGameState CurrentState { get; private set; }

        /// <summary>Gets whether the machine currently has an active state.</summary>
        public bool IsRunning => CurrentState != null;

        /// <summary>Activates a state, exiting the current state first when present.</summary>
        public void ChangeState(IGameState nextState)
        {
            if (nextState == null)
                throw new ArgumentNullException(nameof(nextState));

            TransitionTo(nextState);
        }

        /// <summary>Exits the active state and leaves the machine stopped.</summary>
        public void Stop() => TransitionTo(null);

        /// <summary>Updates the active state, if any.</summary>
        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time must be finite and non-negative.");

            CurrentState?.Tick(deltaTime);
        }

        private void TransitionTo(IGameState nextState)
        {
            if (ReferenceEquals(CurrentState, nextState))
                return;

            if (_isTransitioning)
                throw new InvalidOperationException("A state transition cannot be started from Enter or Exit.");

            IGameState previousState = CurrentState;
            _isTransitioning = true;

            try
            {
                previousState?.Exit();
                CurrentState = nextState;
                CurrentState?.Enter();
            }
            finally
            {
                _isTransitioning = false;
            }

            StateChanged?.Invoke(previousState, CurrentState);
        }
    }
}
