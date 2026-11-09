using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace PixelPlay.Foundation.Networking
{
    /// <summary>Immutable response data returned by an HTTP transport.</summary>
    public sealed class HttpResponseData
    {
        private readonly byte[] _body;

        /// <summary>Creates a response from a status code, body, and headers.</summary>
        public HttpResponseData(int statusCode, byte[] body, IDictionary<string, string> headers = null)
        {
            StatusCode = statusCode;
            _body = body == null ? Array.Empty<byte>() : (byte[])body.Clone();

            var headerCopy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                    headerCopy[header.Key] = header.Value;
            }

            Headers = new ReadOnlyDictionary<string, string>(headerCopy);
        }

        /// <summary>Gets the numeric HTTP status code.</summary>
        public int StatusCode { get; }

        /// <summary>Gets whether the status code is in the 200-299 range.</summary>
        public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode <= 299;

        /// <summary>Gets a copy of the response body.</summary>
        public byte[] Body => (byte[])_body.Clone();

        /// <summary>Gets the response body decoded as UTF-8.</summary>
        public string BodyText => Encoding.UTF8.GetString(_body);

        /// <summary>Gets the response headers.</summary>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>Looks up a response header without case sensitivity.</summary>
        public bool TryGetHeader(string name, out string value) => Headers.TryGetValue(name, out value);
    }
}
