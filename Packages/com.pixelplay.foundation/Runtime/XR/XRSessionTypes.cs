using System;

namespace PixelPlay.Foundation.XR
{
    /// <summary>Describes the kind of immersive session requested from an XR provider.</summary>
    public enum XRSessionMode
    {
        Unknown,
        VirtualReality,
        AugmentedReality,
        MixedReality
    }

    /// <summary>Lifecycle state reported by an XR runtime provider.</summary>
    public enum XRSessionState
    {
        Unavailable,
        Ready,
        Starting,
        Running,
        Paused,
        Stopping,
        Failed
    }

    /// <summary>Describes a runtime XR provider failure.</summary>
    public sealed class XRSessionError : EventArgs
    {
        /// <summary>Creates a provider error with a stable code and readable message.</summary>
        public XRSessionError(string code, string message)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("An error code is required.", nameof(code));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("An error message is required.", nameof(message));
            Code = code;
            Message = message;
        }

        /// <summary>Gets the provider-specific error code.</summary>
        public string Code { get; }

        /// <summary>Gets the readable error message.</summary>
        public string Message { get; }
    }
}
