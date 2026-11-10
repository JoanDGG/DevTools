using System.Collections.Generic;
using PixelPlay.Foundation.Gestures;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Emits mock gestures with changing confidence for the gesture filter.</summary>
    public sealed class MockHandGestureProvider : MonoBehaviour, IHandGestureProvider
    {
        private bool _running;
        private long _frameId;
        private float _elapsed;

        public bool IsRunning => _running;
        public event System.Action<GestureFrame> FrameReceived;
        public event System.Action<GestureProviderError> ErrorReported;

        private void OnEnable() => StartRecognition();
        private void OnDisable() => StopRecognition();
        public void StartRecognition() => _running = true;
        public void StopRecognition() => _running = false;
        public void ReportError(string code, string message) => ErrorReported?.Invoke(new GestureProviderError(code, message));

        private void Update()
        {
            if (!_running)
                return;

            _elapsed += Time.deltaTime;
            string leftGesture = (Mathf.FloorToInt(_elapsed / 2.5f) % 2 == 0) ? "open_palm" : "pinch";
            float confidence = 0.82f + Mathf.Sin(_elapsed * 9f) * 0.08f;
            if (Mathf.Repeat(_elapsed, 4f) < 0.06f)
                confidence = 0.35f;
            var candidates = new List<GestureCandidate>
            {
                new GestureCandidate("left", leftGesture, confidence),
                new GestureCandidate("right", "thumbs_up", 0.92f)
            };
            FrameReceived?.Invoke(new GestureFrame(_frameId++, Time.realtimeSinceStartup, candidates));
        }
    }
}
