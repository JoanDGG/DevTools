using System;

namespace PixelPlay.Foundation.Gestures
{
    /// <summary>A provider-reported hand gesture recognition failure.</summary>
    public readonly struct GestureProviderError
    {
        public GestureProviderError(string code, string message)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Error code must not be null or whitespace.", nameof(code));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Error message must not be null or whitespace.", nameof(message));
            Code = code;
            Message = message;
        }

        public string Code { get; }
        public string Message { get; }
    }
}
