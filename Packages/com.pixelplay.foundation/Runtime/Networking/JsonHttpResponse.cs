namespace PixelPlay.Foundation.Networking
{
    /// <summary>Combines an HTTP response with a successfully deserialized JSON payload.</summary>
    public sealed class JsonHttpResponse<T>
    {
        /// <summary>Creates a response containing the raw HTTP data and optional payload.</summary>
        public JsonHttpResponse(HttpResponseData httpResponse, T payload)
        {
            HttpResponse = httpResponse;
            Payload = payload;
        }

        /// <summary>Gets the raw response, including status code, headers, and body.</summary>
        public HttpResponseData HttpResponse { get; }

        /// <summary>Gets the deserialized payload. It is default when no successful JSON body exists.</summary>
        public T Payload { get; }

        /// <summary>Gets whether the server returned a 2xx status code.</summary>
        public bool IsSuccessStatusCode => HttpResponse.IsSuccessStatusCode;
    }
}
