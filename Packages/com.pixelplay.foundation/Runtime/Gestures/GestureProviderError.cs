using System;

namespace PixelPlay.Foundation.Gestures
{
    /// <summary>A provider-reported hand gesture recognition failure.</summary>
    public readonly struct GestureProviderError
    {
        /// <summary>Creates an error with a stable provider code and readable message.</summary>
        public GestureProviderError(string code, string message)
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
