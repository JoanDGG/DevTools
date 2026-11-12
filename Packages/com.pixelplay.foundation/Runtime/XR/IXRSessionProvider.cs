using System;
using System.Threading;
using System.Threading.Tasks;

namespace PixelPlay.Foundation.XR
{
    /// <summary>Lifecycle contract for Unity XR, AR Foundation, OpenXR, or vendor-specific session providers.</summary>
    /// <remarks>Providers should raise events on Unity's main thread when listeners touch Unity objects.</remarks>
    public interface IXRSessionProvider
    {
        /// <summary>Gets the current provider lifecycle state.</summary>
        XRSessionState State { get; }

        /// <summary>Gets the immersive mode supported by this provider.</summary>
        XRSessionMode Mode { get; }

        /// <summary>Raised whenever the provider changes lifecycle state.</summary>
        event Action<XRSessionState> StateChanged;

        /// <summary>Raised for runtime errors that occur after a session has started.</summary>
        event Action<XRSessionError> ErrorReported;

        /// <summary>Initializes the XR runtime and enters an active session.</summary>
        Task StartAsync(CancellationToken cancellationToken = default);

        /// <summary>Temporarily pauses an active session.</summary>
        Task PauseAsync(CancellationToken cancellationToken = default);

        /// <summary>Resumes a paused session.</summary>
        Task ResumeAsync(CancellationToken cancellationToken = default);

        /// <summary>Stops the session and releases provider resources.</summary>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
