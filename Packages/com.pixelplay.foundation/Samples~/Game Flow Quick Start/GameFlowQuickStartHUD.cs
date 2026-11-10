using System;
using PixelPlay.Foundation.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Reflects game flow state in a simple TextMeshPro HUD and button set.</summary>
    public sealed class GameFlowQuickStartHUD : MonoBehaviour
    {
        [SerializeField] private GameFlowQuickStart source;
        [SerializeField] private TextMeshProUGUI stateLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button finishButton;
        [SerializeField] private Button returnButton;

        private IDisposable _subscription;

        private void OnEnable()
        {
            if (source == null)
                source = GetComponent<GameFlowQuickStart>();
            if (source == null)
            {
                Debug.LogError("Assign a GameFlowQuickStart source.", this);
                return;
            }

            _subscription = source.Events.Subscribe<GameFlowQuickStart.FlowStateChangedEvent>(HandleStateChanged);
            UpdateState(source.CurrentState);
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void HandleStateChanged(GameFlowQuickStart.FlowStateChangedEvent change) => UpdateState(change.State);

        private void UpdateState(GameFlowQuickStart.FlowState state)
        {
            if (stateLabel != null)
                stateLabel.text = state.ToString();
            if (startButton != null) startButton.interactable = state == GameFlowQuickStart.FlowState.MainMenu || state == GameFlowQuickStart.FlowState.Results;
            if (pauseButton != null) pauseButton.interactable = state == GameFlowQuickStart.FlowState.Playing;
            if (resumeButton != null) resumeButton.interactable = state == GameFlowQuickStart.FlowState.Paused;
            if (finishButton != null) finishButton.interactable = state == GameFlowQuickStart.FlowState.Playing;
            if (returnButton != null) returnButton.interactable = state == GameFlowQuickStart.FlowState.Paused || state == GameFlowQuickStart.FlowState.Results;
        }
    }
}
