using PixelPlay.Foundation.Gestures;
using PixelPlay.Foundation.Unity.Gestures;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Logs filtered gesture start and end events.</summary>
    public sealed class GestureRecognitionQuickStartConsumer : MonoBehaviour
    {
        [SerializeField] private GestureRecognitionFilterBehaviour gestureFilter;

        private void OnEnable()
        {
            if (gestureFilter == null)
                gestureFilter = GetComponent<GestureRecognitionFilterBehaviour>();
            if (gestureFilter != null)
                gestureFilter.StateChanged += HandleGestureChanged;
        }

        private void OnDisable()
        {
            if (gestureFilter != null)
                gestureFilter.StateChanged -= HandleGestureChanged;
        }

        private void HandleGestureChanged(GestureStateChange change)
        {
            string action = change.Kind == GestureStateChange.ChangeKind.Started ? "started" : "ended";
            Debug.Log($"Gesture {action}: {change.GestureId} ({change.HandId}, confidence {change.Confidence:0.00}).", this);
        }
    }
}
