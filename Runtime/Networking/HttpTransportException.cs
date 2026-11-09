using System;

namespace PixelPlay.Foundation.Networking
{
    /// <summary>Represents a failure to communicate with an HTTP server.</summary>
    public sealed class HttpTransportException : Exception
    {
        /// <summary>Creates a transport exception for the specified request URL.</summary>
        public HttpTransportException(string url, string message, Exception innerException = null)
            : base($"HTTP transport failed for '{url}': {message}", innerException)
        {
            Url = url;
        }

        /// <summary>Gets the URL of the request that failed.</summary>
        public string Url { get; }
    }
}
