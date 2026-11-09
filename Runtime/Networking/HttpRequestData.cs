using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PixelPlay.Foundation.Networking
{
    /// <summary>Immutable input data for an HTTP request.</summary>
    public sealed class HttpRequestData
    {
        private readonly byte[] _body;

        /// <summary>Creates an HTTP request description. A timeout of zero disables the timeout.</summary>
        public HttpRequestData(
            string url,
            string method = "GET",
            byte[] body = null,
            IDictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL must not be null or whitespace.", nameof(url));
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentException("HTTP method must not be null or whitespace.", nameof(method));
            if (timeoutSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "Timeout must be zero or greater.");

            Url = url;
            Method = method;
            TimeoutSeconds = timeoutSeconds;
            _body = body == null ? Array.Empty<byte>() : (byte[])body.Clone();

            var headerCopy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    if (string.IsNullOrWhiteSpace(header.Key))
                        throw new ArgumentException("Header names must not be null or whitespace.", nameof(headers));
                    if (header.Value == null)
                        throw new ArgumentException("Header values must not be null.", nameof(headers));

                    headerCopy.Add(header.Key, header.Value);
                }
            }

            Headers = new ReadOnlyDictionary<string, string>(headerCopy);
        }

        /// <summary>Gets the request URL.</summary>
        public string Url { get; }

        /// <summary>Gets the HTTP method, such as GET or POST.</summary>
        public string Method { get; }

        /// <summary>Gets a copy of the request body.</summary>
        public byte[] Body => (byte[])_body.Clone();

        /// <summary>Gets the request headers.</summary>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>Gets the timeout in seconds. Zero means no timeout.</summary>
        public int TimeoutSeconds { get; }
    }
}
