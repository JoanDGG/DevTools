using PixelPlay.Foundation.Debugging;
using PixelPlay.Foundation.Unity.Debugging;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Demonstrates game code reading and reacting to tunable round values.</summary>
    public sealed class RuntimeDebugQuickStartConsumer : MonoBehaviour
    {
        [SerializeField, Tooltip("Registry used to read the round duration and round count settings.")] private RuntimeTuningRegistryBehaviour tuningRegistry;
        private FloatTunable _duration;
        private IntegerTunable _roundCount;

        private void Awake()
        {
            if (tuningRegistry == null)
                tuningRegistry = GetComponent<RuntimeTuningRegistryBehaviour>();
            if (tuningRegistry == null)
            {
                Debug.LogError("Assign a RuntimeTuningRegistryBehaviour.", this);
                enabled = false;
                return;
            }

            RuntimeTuningRegistry registry = tuningRegistry.Registry;
            if (!registry.Contains("round.duration"))
                registry.AddFloat("round.duration", "Round Duration", 60f, 10f, 180f, "Gameplay");
            if (!registry.Contains("round.count"))
                registry.AddInteger("round.count", "Round Count", 3, 1, 10, "Gameplay");
            _duration = registry.GetFloat("round.duration");
            _roundCount = registry.GetInteger("round.count");
        }

        private void OnEnable()
        {
            if (_duration != null) _duration.ValueChanged += HandleChanged;
            if (_roundCount != null) _roundCount.ValueChanged += HandleChanged;
        }

        private void OnDisable()
        {
            if (_duration != null) _duration.ValueChanged -= HandleChanged;
            if (_roundCount != null) _roundCount.ValueChanged -= HandleChanged;
        }

        [ContextMenu("Log Current Game Settings")]
        private void LogCurrentSettings()
        {
            if (_duration != null && _roundCount != null)
                Debug.Log($"Match: {_roundCount.Value} rounds x {_duration.Value:0.#} seconds.", this);
        }

        private void HandleChanged(RuntimeTunable changed) => Debug.Log($"Tuned {changed.DisplayName}.", this);
    }
}
