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
        [SerializeField, Tooltip("Flow controller whose state and status events update this HUD.")] private GameFlowQuickStart source;
        [SerializeField, Tooltip("TextMeshPro label displaying the current game state and latest status.")] private TextMeshProUGUI stateLabel;
        [SerializeField, Tooltip("Starts a round or restarts it after returning to the menu.")] private Button startButton;
        [SerializeField, Tooltip("Pauses the active round.")] private Button pauseButton;
        [SerializeField, Tooltip("Resumes a paused round.")] private Button resumeButton;
        [SerializeField, Tooltip("Ends the active round and moves to the results state.")] private Button finishButton;
        [SerializeField, Tooltip("Returns from results to the menu.")] private Button returnButton;

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

            SubscribeButtons();
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        private void SubscribeButtons()
        {
            if (startButton != null) startButton.onClick.AddListener(source.StartGame);
            if (pauseButton != null) pauseButton.onClick.AddListener(source.PauseGame);
            if (resumeButton != null) resumeButton.onClick.AddListener(source.ResumeGame);
            if (finishButton != null) finishButton.onClick.AddListener(source.FinishGame);
            if (returnButton != null) returnButton.onClick.AddListener(source.ReturnToMenu);
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
