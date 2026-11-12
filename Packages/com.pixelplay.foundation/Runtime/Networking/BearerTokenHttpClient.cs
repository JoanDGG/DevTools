using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PixelPlay.Foundation.Networking
{
    /// <summary>Decorates an HTTP client with a bearer access token on every request.</summary>
    /// <remarks>The token provider owns sign-in, token storage, and refresh behavior.</remarks>
    public sealed class BearerTokenHttpClient : IHttpClient
    {
        private readonly IHttpClient _innerClient;
        private readonly IAccessTokenProvider _tokenProvider;

        /// <summary>Creates an authenticated client around another HTTP transport.</summary>
        public BearerTokenHttpClient(IHttpClient innerClient, IAccessTokenProvider tokenProvider)
        {
            _innerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        }

        /// <inheritdoc />
        public async Task<HttpResponseData> SendAsync(
            HttpRequestData request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            string accessToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
            ValidateToken(accessToken);

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> header in request.Headers)
                headers.Add(header.Key, header.Value);

            // Replace any caller-provided value so the token provider remains authoritative.
            headers["Authorization"] = "Bearer " + accessToken;

            var authenticatedRequest = new HttpRequestData(
                request.Url,
                request.Method,
                request.Body,
                headers,
                request.TimeoutSeconds);

            return await _innerClient.SendAsync(authenticatedRequest, cancellationToken);
        }

        private static void ValidateToken(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("The access token provider returned an empty token.");

            foreach (char character in accessToken)
            {
                if (char.IsWhiteSpace(character) || char.IsControl(character))
                    throw new InvalidOperationException("The access token contains an invalid whitespace or control character.");
            }
        }
    }
}
