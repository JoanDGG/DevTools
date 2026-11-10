using System;

namespace PixelPlay.Foundation.Tracking
{
    /// <summary>Lifecycle and result contract implemented by a body or hand tracking backend.</summary>
    /// <remarks>Implementations should raise events on the Unity main thread when their consumers touch Unity objects.</remarks>
    public interface ITrackingProvider
    {
        /// <summary>Gets whether the provider is actively producing tracking results.</summary>
        bool IsRunning { get; }

        /// <summary>Raised when a new immutable result frame is available.</summary>
        event Action<TrackingFrame> FrameReceived;

        /// <summary>Raised when the provider encounters an error.</summary>
        event Action<TrackingError> ErrorReported;

        /// <summary>Starts the provider.</summary>
        void StartTracking();

        /// <summary>Stops the provider and releases its active tracking session.</summary>
        void StopTracking();
    }
}
