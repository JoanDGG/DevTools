using PixelPlay.Foundation.Events;
using PixelPlay.Foundation.StateMachine;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Demonstrates a menu, playing, pause, and results flow with typed events.</summary>
    public sealed class GameFlowQuickStart : MonoBehaviour
    {
        /// <summary>High-level states used by the sample game flow.</summary>
        public enum FlowState { MainMenu, Playing, Paused, Results }

        private readonly IEventBus _events = new EventBus();
        private GameStateMachine _machine;
        private IGameState _menu;
        private IGameState _playing;
        private IGameState _paused;
        private IGameState _results;

        /// <summary>Gets the event bus publishing state-change notifications.</summary>
        public IEventBus Events => _events;
        /// <summary>Gets the current demo flow state.</summary>
        public FlowState CurrentState { get; private set; }

        /// <summary>Event payload published whenever the active state changes.</summary>
        public readonly struct FlowStateChangedEvent
        {
            /// <summary>Creates an event for the supplied state.</summary>
            public FlowStateChangedEvent(FlowState state) => State = state;
            /// <summary>Gets the newly active state.</summary>
            public FlowState State { get; }
        }

        private void Awake()
        {
            _machine = new GameStateMachine();
            _menu = new DemoState(this, FlowState.MainMenu, "Main menu");
            _playing = new DemoState(this, FlowState.Playing, "Playing");
            _paused = new DemoState(this, FlowState.Paused, "Paused");
            _results = new DemoState(this, FlowState.Results, "Results");
            _machine.ChangeState(_menu);
        }

        private void Update() => _machine.Tick(Time.deltaTime);

        /// <summary>Starts or restarts the demo round from the menu or results screen.</summary>
        public void StartGame()
        {
            if (CurrentState == FlowState.MainMenu || CurrentState == FlowState.Results)
                _machine.ChangeState(_playing);
        }

        /// <summary>Pauses the active round.</summary>
        public void PauseGame()
        {
            if (CurrentState == FlowState.Playing)
                _machine.ChangeState(_paused);
        }

        /// <summary>Resumes a paused round.</summary>
        public void ResumeGame()
        {
            if (CurrentState == FlowState.Paused)
                _machine.ChangeState(_playing);
        }

        /// <summary>Ends the active round and displays the results state.</summary>
        public void FinishGame()
        {
            if (CurrentState == FlowState.Playing)
                _machine.ChangeState(_results);
        }

        /// <summary>Returns to the menu from the paused or results state.</summary>
        public void ReturnToMenu()
        {
            if (CurrentState == FlowState.Paused || CurrentState == FlowState.Results)
                _machine.ChangeState(_menu);
        }

        private void SetState(FlowState state)
        {
            CurrentState = state;
            Debug.Log($"Game flow state: {state}.", this);
            _events.Publish(new FlowStateChangedEvent(state));
        }

        private void OnDestroy()
        {
            if (_machine != null)
                _machine.Stop();
            _events.Clear();
        }

        private sealed class DemoState : IGameState
        {
            private readonly GameFlowQuickStart _owner;
            private readonly FlowState _state;
            private readonly string _label;

            public DemoState(GameFlowQuickStart owner, FlowState state, string label)
            {
                _owner = owner;
                _state = state;
                _label = label;
            }

            public void Enter()
            {
                Debug.Log($"Entering {_label}.", _owner);
                _owner.SetState(_state);
            }

            public void Exit() { }
            public void Tick(float deltaTime) { }
        }
    }
}
