using System;

namespace PixelPlay.Foundation.Tracking
{
    /// <summary>A provider-reported tracking failure.</summary>
    public readonly struct TrackingError
    {
        /// <summary>Creates a tracking error with a stable code and readable message.</summary>
        public TrackingError(string code, string message)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Error code must not be null or whitespace.", nameof(code));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Error message must not be null or whitespace.", nameof(message));

            Code = code;
            Message = message;
        }

        /// <summary>Gets the provider-specific error code.</summary>
        public string Code { get; }
        /// <summary>Gets the readable error message.</summary>
        public string Message { get; }
    }
}
