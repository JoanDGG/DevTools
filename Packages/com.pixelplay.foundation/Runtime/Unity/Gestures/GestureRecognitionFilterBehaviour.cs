using System;
using PixelPlay.Foundation.Gestures;
using UnityEngine;
using UnityEngine.Events;

namespace PixelPlay.Foundation.Unity.Gestures
{
    /// <summary>Connects a hand gesture provider to the confidence and stability filter.</summary>
    public sealed class GestureRecognitionFilterBehaviour : MonoBehaviour
    {
        [Serializable]
        public sealed class GestureStateEvent : UnityEvent<string, string, bool> { }

        [SerializeField] private MonoBehaviour providerComponent;
        [SerializeField] private bool startProviderOnEnable;
        [SerializeField] private bool stopProviderOnDisable;
        [SerializeField, Range(0f, 1f)] private float startConfidence = 0.7f;
        [SerializeField, Range(0f, 1f)] private float releaseConfidence = 0.45f;
        [SerializeField, Min(0f)] private float activationSeconds = 0.12f;
        [SerializeField, Min(0f)] private float releaseSeconds = 0.18f;
        [SerializeField] private GestureStateEvent onGestureStateChanged = new GestureStateEvent();
        [SerializeField] private UnityEvent<string> onProviderError = new UnityEvent<string>();

        private IHandGestureProvider _provider;
        private GestureRecognitionFilter _filter;

        /// <summary>Gets the event raised when a gesture starts or ends.</summary>
        public GestureStateEvent OnGestureStateChanged => onGestureStateChanged;

        /// <summary>Gets the event raised when the recognition provider reports an error.</summary>
        public UnityEvent<string> OnProviderError => onProviderError;

        /// <summary>Raised when a gesture starts or ends.</summary>
        public event Action<GestureStateChange> StateChanged;

        /// <summary>Raised when the recognition provider reports an error.</summary>
        public event Action<GestureProviderError> ProviderErrorReported;

        private void OnEnable()
        {
            _provider = providerComponent as IHandGestureProvider;
            if (_provider == null)
            {
                Debug.LogError("Assign a component that implements IHandGestureProvider.", this);
                return;
            }

            try
            {
                _filter = new GestureRecognitionFilter(startConfidence, releaseConfidence,
                    activationSeconds, releaseSeconds);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                Debug.LogError($"Invalid gesture filter settings: {exception.Message}", this);
                _provider = null;
                enabled = false;
                return;
            }
            catch (ArgumentException exception)
            {
                Debug.LogError($"Invalid gesture filter settings: {exception.Message}", this);
                _provider = null;
                enabled = false;
                return;
            }
            _filter.StateChanged += HandleStateChanged;
            _provider.FrameReceived += HandleFrameReceived;
            _provider.ErrorReported += HandleProviderError;

            if (startProviderOnEnable)
                _provider.StartRecognition();
        }

        private void OnDisable()
        {
            if (_provider != null)
            {
                _provider.FrameReceived -= HandleFrameReceived;
                _provider.ErrorReported -= HandleProviderError;
            }

            if (_filter != null)
            {
                _filter.Reset(Time.realtimeSinceStartup);
                _filter.StateChanged -= HandleStateChanged;
                _filter = null;
            }

            if (stopProviderOnDisable && _provider != null)
                _provider.StopRecognition();

            _provider = null;
        }

        private void HandleFrameReceived(GestureFrame frame) => _filter?.Process(frame);

        private void HandleStateChanged(GestureStateChange change)
        {
            StateChanged?.Invoke(change);
            onGestureStateChanged?.Invoke(change.HandId, change.GestureId,
                change.Kind == GestureStateChange.ChangeKind.Started);
        }

        private void HandleProviderError(GestureProviderError error)
        {
            ProviderErrorReported?.Invoke(error);
            onProviderError?.Invoke($"{error.Code}: {error.Message}");
        }
    }
}
