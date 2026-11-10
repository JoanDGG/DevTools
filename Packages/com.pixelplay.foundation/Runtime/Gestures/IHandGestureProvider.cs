using System;

namespace PixelPlay.Foundation.Gestures
{
    /// <summary>Lifecycle and result contract implemented by a hand gesture recognition backend.</summary>
    /// <remarks>Raise callbacks on the Unity main thread when listeners interact with Unity objects.</remarks>
    public interface IHandGestureProvider
    {
        /// <summary>Gets whether the provider is actively recognizing hand gestures.</summary>
        bool IsRunning { get; }

        /// <summary>Raised when a new set of gesture candidates is available.</summary>
        event Action<GestureFrame> FrameReceived;

        /// <summary>Raised when the provider encounters an error.</summary>
        event Action<GestureProviderError> ErrorReported;

        /// <summary>Starts recognition.</summary>
        void StartRecognition();

        /// <summary>Stops recognition and releases the active provider session.</summary>
        void StopRecognition();
    }
}
