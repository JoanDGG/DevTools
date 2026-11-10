using System.Collections.Generic;
using PixelPlay.Foundation.Tracking;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Emits a moving mock body skeleton for the tracking debug overlay.</summary>
    public sealed class MockBodyTrackingProvider : MonoBehaviour, ITrackingProvider
    {
        private bool _running;
        private long _frameId;
        private float _elapsed;

        public bool IsRunning => _running;
        public event System.Action<TrackingFrame> FrameReceived;
        public event System.Action<TrackingError> ErrorReported;

        private void OnEnable() => StartTracking();
        private void OnDisable() => StopTracking();
        public void StartTracking() => _running = true;
        public void StopTracking() => _running = false;

        public void ReportError(string code, string message) =>
            ErrorReported?.Invoke(new TrackingError(code, message));

        private void Update()
        {
            if (!_running)
                return;

            _elapsed += Time.deltaTime;
            float center = 0.5f + Mathf.Sin(_elapsed * 0.45f) * 0.08f;
            float arm = Mathf.Sin(_elapsed * 1.6f) * 0.1f;
            var points = new List<TrackedLandmark>
            {
                Point("nose", center, 0.16f), Point("left_shoulder", center - 0.1f, 0.3f),
                Point("right_shoulder", center + 0.1f, 0.3f), Point("left_elbow", center - 0.19f, 0.43f + arm),
                Point("right_elbow", center + 0.19f, 0.43f - arm), Point("left_wrist", center - 0.24f, 0.56f + arm),
                Point("right_wrist", center + 0.24f, 0.56f - arm), Point("left_hip", center - 0.07f, 0.57f),
                Point("right_hip", center + 0.07f, 0.57f), Point("left_knee", center - 0.08f, 0.74f),
                Point("right_knee", center + 0.08f, 0.74f), Point("left_ankle", center - 0.09f, 0.9f),
                Point("right_ankle", center + 0.09f, 0.9f)
            };
            var person = new TrackedPerson("mock-person", points);
            FrameReceived?.Invoke(new TrackingFrame(_frameId++, Time.realtimeSinceStartup, 1280, 720, new[] { person }));
        }

        private static TrackedLandmark Point(string id, float x, float y) => new TrackedLandmark(id, x, y, 0f, 0.95f);
    }
}
