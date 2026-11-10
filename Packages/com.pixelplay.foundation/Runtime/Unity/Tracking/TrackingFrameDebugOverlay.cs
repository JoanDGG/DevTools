using PixelPlay.Foundation.Tracking;
using UnityEngine;

namespace PixelPlay.Foundation.Unity.Tracking
{
    /// <summary>Draws normalized tracking landmarks over the game view for development.</summary>
    public sealed class TrackingFrameDebugOverlay : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour providerComponent;
        [SerializeField, Range(0f, 1f)] private float minimumConfidence = 0.2f;
        [SerializeField] private bool showLandmarkIds = true;
        [SerializeField, Min(2f)] private float markerSize = 10f;

        private readonly object _frameLock = new object();
        private ITrackingProvider _provider;
        private TrackingFrame _latestFrame;
        private string _latestError;
        private GUIStyle _statusStyle;

        private void OnEnable()
        {
            _provider = providerComponent as ITrackingProvider;
            if (_provider == null)
            {
                Debug.LogError("Assign a component that implements ITrackingProvider.", this);
                return;
            }

            _provider.FrameReceived += HandleFrameReceived;
            _provider.ErrorReported += HandleErrorReported;
        }

        private void OnDisable()
        {
            if (_provider == null)
                return;

            _provider.FrameReceived -= HandleFrameReceived;
            _provider.ErrorReported -= HandleErrorReported;
            _provider = null;
        }

        private void HandleFrameReceived(TrackingFrame frame)
        {
            lock (_frameLock)
                _latestFrame = frame;
        }

        private void HandleErrorReported(TrackingError error)
        {
            lock (_frameLock)
                _latestError = $"{error.Code}: {error.Message}";
        }

        private void OnGUI()
        {
            TrackingFrame frame;
            string error;
            lock (_frameLock)
            {
                frame = _latestFrame;
                error = _latestError;
            }

            EnsureStatusStyle();
            Color previousColor = GUI.color;
            GUI.color = Color.white;
            if (!string.IsNullOrEmpty(error))
                GUI.Label(new Rect(12f, 12f, Screen.width - 24f, 28f), error, _statusStyle);
            else if (frame == null)
                GUI.Label(new Rect(12f, 12f, Screen.width - 24f, 28f), "Waiting for tracking frames...", _statusStyle);
            else
                GUI.Label(new Rect(12f, 12f, Screen.width - 24f, 28f),
                    $"Frame {frame.FrameId} | People: {frame.Persons.Count}", _statusStyle);

            if (frame == null)
            {
                GUI.color = previousColor;
                return;
            }

            for (int personIndex = 0; personIndex < frame.Persons.Count; personIndex++)
            {
                TrackedPerson person = frame.Persons[personIndex];
                Color personColor = Color.HSVToRGB((personIndex * 0.23f) % 1f, 0.8f, 1f);
                for (int landmarkIndex = 0; landmarkIndex < person.Landmarks.Count; landmarkIndex++)
                {
                    TrackedLandmark landmark = person.Landmarks[landmarkIndex];
                    if (landmark.Confidence < minimumConfidence)
                        continue;

                    float x = landmark.NormalizedX * Screen.width;
                    float y = landmark.NormalizedY * Screen.height;
                    GUI.color = personColor;
                    GUI.DrawTexture(new Rect(x - markerSize * 0.5f, y - markerSize * 0.5f, markerSize, markerSize),
                        Texture2D.whiteTexture);
                    if (showLandmarkIds)
                    {
                        GUI.Label(new Rect(x + markerSize * 0.5f, y - markerSize * 0.5f, 140f, 22f),
                            $"{person.Id}:{landmark.Id}", _statusStyle);
                    }
                }
            }

            GUI.color = previousColor;
        }

        private void EnsureStatusStyle()
        {
            if (_statusStyle != null)
                return;

            _statusStyle = new GUIStyle(GUI.skin.label);
            _statusStyle.fontSize = 14;
            _statusStyle.fontStyle = FontStyle.Bold;
            _statusStyle.normal.textColor = Color.white;
        }
    }
}
