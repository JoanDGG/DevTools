using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PixelPlay.Foundation.Networking
{
    /// <summary>Builds JSON requests and deserializes successful JSON responses.</summary>
    public sealed class JsonHttpClient
    {
        private const string JsonContentType = "application/json; charset=utf-8";

        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>Creates a JSON client from an HTTP transport and a serializer.</summary>
        public JsonHttpClient(IHttpClient httpClient, IJsonSerializer jsonSerializer)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _jsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        }

        /// <summary>Sends an existing request and deserializes a successful non-empty JSON response.</summary>
        public async Task<JsonHttpResponse<TResponse>> SendAsync<TResponse>(
            HttpRequestData request,
            CancellationToken cancellationToken = default)
        {
            HttpResponseData response = await _httpClient.SendAsync(request, cancellationToken);
            TResponse payload = default;

            if (response.IsSuccessStatusCode && response.Body.Length > 0)
                payload = _jsonSerializer.Deserialize<TResponse>(response.BodyText);

            return new JsonHttpResponse<TResponse>(response, payload);
        }

        /// <summary>Creates an HTTP request with a JSON-serialized body and content type.</summary>
        public HttpRequestData CreateJsonRequest<TBody>(
            string url,
            string method,
            TBody body,
            IDictionary<string, string> headers = null,
            int timeoutSeconds = 30)
        {
            string json = _jsonSerializer.Serialize(body);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(json);
            var requestHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                    requestHeaders.Add(header.Key, header.Value);
            }

            if (!requestHeaders.ContainsKey("Content-Type"))
                requestHeaders.Add("Content-Type", JsonContentType);

            return new HttpRequestData(url, method, bodyBytes, requestHeaders, timeoutSeconds);
        }

        /// <summary>Serializes a request body, sends it, and deserializes a successful JSON response.</summary>
        public async Task<JsonHttpResponse<TResponse>> SendJsonAsync<TRequest, TResponse>(
            string url,
            string method,
            TRequest body,
            IDictionary<string, string> headers = null,
            int timeoutSeconds = 30,
            CancellationToken cancellationToken = default)
        {
            HttpRequestData request = CreateJsonRequest(url, method, body, headers, timeoutSeconds);
            return await SendAsync<TResponse>(request, cancellationToken);
        }
    }
}
