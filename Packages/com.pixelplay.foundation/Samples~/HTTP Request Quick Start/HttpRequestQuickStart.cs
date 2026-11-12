using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PixelPlay.Foundation.Networking;
using PixelPlay.Foundation.Unity.Networking;
using UnityEngine;
using UnityEngine.Events;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Sends a configurable HTTP request through the package's Unity transport.</summary>
    public sealed class HttpRequestQuickStart : MonoBehaviour
    {
        /// <summary>HTTP methods exposed by the Inspector sample.</summary>
        public enum RequestMethod
        {
            /// <summary>Reads a resource without sending a request body.</summary>
            Get,
            /// <summary>Creates or submits a resource.</summary>
            Post,
            /// <summary>Replaces a resource.</summary>
            Put,
            /// <summary>Partially updates a resource.</summary>
            Patch,
            /// <summary>Deletes a resource.</summary>
            Delete,
            /// <summary>Requests headers without a response body.</summary>
            Head
        }

        /// <summary>Unity event invoked with the response status code and body text.</summary>
        [Serializable]
        public sealed class ResponseEvent : UnityEvent<int, string> { }

        [Header("Request")]
        [SerializeField, Tooltip("Destination URL. Configure your API endpoint here before sending a request.")]
        private string endpointUrl;
        [SerializeField, Tooltip("HTTP method used for the request.")]
        private RequestMethod method = RequestMethod.Get;
        [SerializeField, Tooltip("Content type for a non-empty request body, commonly application/json.")]
        private string contentType = "application/json";
        [SerializeField, TextArea(4, 12), Tooltip("Raw request body for POST, PUT, and PATCH. Paste JSON here when required by the endpoint.")]
        private string requestBody;
        [SerializeField, Min(0), Tooltip("Request timeout in seconds; zero disables the timeout.")]
        private int timeoutSeconds = 30;

        [Header("Optional Authentication")]
        [SerializeField, Tooltip("Optional component implementing IAccessTokenProvider. Leave empty for a public endpoint.")]
        private MonoBehaviour accessTokenProviderComponent;

        [Header("Events")]
        [SerializeField, Tooltip("Invoked for every HTTP response, including 4xx and 5xx status codes.")]
        private ResponseEvent onResponse = new ResponseEvent();
        [SerializeField, Tooltip("Invoked when validation or network transport fails; receives an error message.")]
        private UnityEvent<string> onError = new UnityEvent<string>();

        private bool _isSending;

        /// <summary>Gets the endpoint used by the next request.</summary>
        public string EndpointUrl => endpointUrl;

        /// <summary>Sets the endpoint URL used by subsequent requests.</summary>
        /// <param name="value">Absolute HTTP or HTTPS endpoint; whitespace around it is trimmed.</param>
        public void SetEndpointUrl(string value) => endpointUrl = value == null ? string.Empty : value.Trim();

        /// <summary>Sets the HTTP method used by subsequent requests.</summary>
        public void SetRequestMethod(RequestMethod value) => method = value;

        /// <summary>Sets the method from a Unity UI Dropdown index using the enum declaration order.</summary>
        public void SetRequestMethodByIndex(int index)
        {
            if (!Enum.IsDefined(typeof(RequestMethod), index))
                throw new ArgumentOutOfRangeException(nameof(index), "Select a supported HTTP method index.");
            method = (RequestMethod)index;
        }

        /// <summary>Sets the raw body used by POST, PUT, PATCH, or DELETE requests.</summary>
        public void SetRequestBody(string value) => requestBody = value ?? string.Empty;

        /// <summary>Sets the Content-Type header used when the request body is non-empty.</summary>
        public void SetContentType(string value) => contentType = value == null ? string.Empty : value.Trim();

        /// <summary>Sets the request timeout in seconds. Zero disables the timeout.</summary>
        public void SetTimeoutSeconds(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Timeout must be zero or greater.");
            timeoutSeconds = value;
        }

        /// <summary>Assigns an optional component implementing IAccessTokenProvider.</summary>
        public void SetAccessTokenProvider(MonoBehaviour providerComponent)
        {
            if (providerComponent != null && !(providerComponent is IAccessTokenProvider))
                throw new ArgumentException("The component must implement IAccessTokenProvider.", nameof(providerComponent));
            accessTokenProviderComponent = providerComponent;
        }

        /// <summary>Gets the event raised for HTTP responses; arguments are status code and response body.</summary>
        public ResponseEvent OnResponse => onResponse;

        /// <summary>Gets the event raised when request setup or transport fails.</summary>
        public UnityEvent<string> OnError => onError;

        /// <summary>Sends the request configured in the Inspector; can be wired to a UI Button.</summary>
        [ContextMenu("Send Configured HTTP Request")]
        public async void SendConfiguredRequest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before sending the sample HTTP request.", this);
                return;
            }

            if (_isSending)
            {
                Debug.LogWarning("An HTTP request is already in progress.", this);
                return;
            }

            _isSending = true;
            try
            {
                HttpResponseData response = await SendConfiguredRequestAsync();
                onResponse?.Invoke(response.StatusCode, response.BodyText);
                if (!response.IsSuccessStatusCode)
                    Debug.LogWarning($"HTTP request returned status {response.StatusCode}: {response.BodyText}", this);
                else
                    Debug.Log($"HTTP request succeeded with status {response.StatusCode}.", this);
            }
            catch (Exception exception)
            {
                onError?.Invoke(exception.Message);
                Debug.LogError($"HTTP request failed: {exception.Message}", this);
            }
            finally
            {
                _isSending = false;
            }
        }

        /// <summary>Sends the configured request and returns its HTTP response.</summary>
        /// <returns>Response data, including non-success HTTP status codes.</returns>
        /// <exception cref="InvalidOperationException">The URL, method settings, or optional token provider are invalid.</exception>
        /// <exception cref="HttpTransportException">The request could not reach the server or its response could not be read.</exception>
        public async Task<HttpResponseData> SendConfiguredRequestAsync()
        {
            if (string.IsNullOrWhiteSpace(endpointUrl))
                throw new InvalidOperationException("Configure an endpoint URL in the Inspector before sending a request.");
            if (timeoutSeconds < 0)
                throw new InvalidOperationException("Timeout must be zero or greater.");

            string methodName = method.ToString().ToUpperInvariant();
            byte[] body = GetBodyBytes(method);
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (body.Length > 0 && !string.IsNullOrWhiteSpace(contentType))
                headers.Add("Content-Type", contentType.Trim());

            var request = new HttpRequestData(endpointUrl.Trim(), methodName, body, headers, timeoutSeconds);
            IHttpClient httpClient = new UnityWebRequestHttpClient();

            if (accessTokenProviderComponent != null)
            {
                if (!(accessTokenProviderComponent is IAccessTokenProvider tokenProvider))
                    throw new InvalidOperationException("The authentication component must implement IAccessTokenProvider.");
                ValidateAuthenticatedEndpoint(endpointUrl);
                httpClient = new BearerTokenHttpClient(httpClient, tokenProvider);
            }

            return await httpClient.SendAsync(request);
        }

        private byte[] GetBodyBytes(RequestMethod requestMethod)
        {
            if (requestMethod == RequestMethod.Get || requestMethod == RequestMethod.Head ||
                string.IsNullOrEmpty(requestBody))
                return Array.Empty<byte>();

            return System.Text.Encoding.UTF8.GetBytes(requestBody);
        }

        private static void ValidateAuthenticatedEndpoint(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                throw new InvalidOperationException("The endpoint must be an absolute URL when authentication is enabled.");
            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && !uri.IsLoopback)
                throw new InvalidOperationException("Authenticated requests require HTTPS; HTTP is allowed for localhost testing.");
        }
    }
}
