using System;
using System.Threading;
using System.Threading.Tasks;
using PixelPlay.Foundation.Networking;
using UnityEngine;
using UnityEngine.Events;

namespace PixelPlay.Foundation.Unity.Networking
{
    /// <summary>Uploads PNG image data as multipart/form-data through the package HTTP client.</summary>
    public sealed class MultipartImageUploader : MonoBehaviour
    {
        /// <summary>Unity event with an HTTP status code and response body.</summary>
        [Serializable]
        public sealed class ResponseEvent : UnityEvent<int, string> { }

        /// <summary>Additional text field sent with the uploaded image.</summary>
        [Serializable]
        private sealed class FormField
        {
            [Tooltip("Multipart field name expected by the endpoint.")]
            public string name;
            [Tooltip("Text value sent for this field.")]
            public string value;
        }

        [SerializeField, Tooltip("HTTPS endpoint that accepts a multipart/form-data POST request.")] private string endpointUrl;
        [SerializeField, Tooltip("Multipart field name used for the image file part.")] private string imageFieldName = "image";
        [SerializeField, Tooltip("Filename used when UploadPngAsync is not given an explicit name.")] private string defaultFileName = "capture.png";
        [SerializeField, Min(0), Tooltip("Request timeout in seconds; zero disables the timeout.")] private int timeoutSeconds = 30;
        [SerializeField, Tooltip("Optional text fields included with every upload.")] private FormField[] additionalFields = Array.Empty<FormField>();
        [SerializeField, Tooltip("Optional component implementing IAccessTokenProvider.")]
        private MonoBehaviour accessTokenProviderComponent;
        [SerializeField, Tooltip("Invoked for every HTTP response, including non-success status codes.")] private ResponseEvent onResponse = new ResponseEvent();
        [SerializeField, Tooltip("Invoked when request creation or network transport fails; receives the error message.")] private UnityEvent<string> onError = new UnityEvent<string>();

        /// <summary>Gets the event raised for any HTTP response, including non-success status codes.</summary>
        public ResponseEvent OnResponse => onResponse;

        /// <summary>Gets the event raised when request creation or transport fails.</summary>
        public UnityEvent<string> OnError => onError;

        /// <summary>Sets the endpoint URL used by subsequent uploads.</summary>
        public void SetEndpointUrl(string value) => endpointUrl = value?.Trim() ?? string.Empty;

        /// <summary>Sets the multipart form field name used for the uploaded image.</summary>
        public void SetImageFieldName(string value) => imageFieldName = value?.Trim() ?? string.Empty;

        /// <summary>Sets the fallback filename used when an upload call omits one.</summary>
        public void SetDefaultFileName(string value) => defaultFileName = value?.Trim() ?? string.Empty;

        /// <summary>Sets the upload timeout in seconds; zero disables the timeout.</summary>
        public void SetTimeoutSeconds(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Timeout must be zero or greater.");
            timeoutSeconds = value;
        }

        /// <summary>Assigns a component that implements <see cref="IAccessTokenProvider"/>.</summary>
        public void SetAccessTokenProvider(MonoBehaviour component)
        {
            if (component != null && !(component is IAccessTokenProvider))
                throw new ArgumentException("The assigned component must implement IAccessTokenProvider.", nameof(component));
            accessTokenProviderComponent = component;
        }

        /// <summary>Uploads PNG bytes and returns the HTTP response.</summary>
        public async Task<HttpResponseData> UploadPngAsync(
            byte[] pngBytes,
            string fileName = null,
            CancellationToken cancellationToken = default)
        {
            if (pngBytes == null || pngBytes.Length == 0)
                throw new ArgumentException("PNG data must contain at least one byte.", nameof(pngBytes));
            if (string.IsNullOrWhiteSpace(endpointUrl))
                throw new InvalidOperationException("Configure an upload endpoint URL first.");
            if (string.IsNullOrWhiteSpace(imageFieldName))
                throw new InvalidOperationException("Configure the multipart image field name first.");
            if (timeoutSeconds < 0)
                throw new InvalidOperationException("Timeout must be zero or greater.");

            IAccessTokenProvider tokenProvider = GetTokenProvider();
            ValidateEndpoint(tokenProvider != null);

            string resolvedFileName = string.IsNullOrWhiteSpace(fileName) ? defaultFileName : fileName;
            if (string.IsNullOrWhiteSpace(resolvedFileName))
                resolvedFileName = "capture.png";

            try
            {
                var form = new MultipartFormDataBuilder()
                    .AddFile(imageFieldName, resolvedFileName, "image/png", pngBytes);

                if (additionalFields != null)
                {
                    foreach (FormField field in additionalFields)
                    {
                        if (field == null)
                            continue;
                        if (string.IsNullOrWhiteSpace(field.name))
                            throw new InvalidOperationException("Every additional multipart field needs a name.");
                        form.AddField(field.name, field.value ?? string.Empty);
                    }
                }

                HttpRequestData request = form.BuildRequest(endpointUrl, "POST", timeoutSeconds: timeoutSeconds);
                IHttpClient httpClient = new UnityWebRequestHttpClient();
                if (tokenProvider != null)
                    httpClient = new BearerTokenHttpClient(httpClient, tokenProvider);

                HttpResponseData response = await httpClient.SendAsync(request, cancellationToken);
                onResponse?.Invoke(response.StatusCode, response.BodyText);
                return response;
            }
            catch (Exception exception)
            {
                onError?.Invoke(exception.Message);
                throw;
            }
        }

        private IAccessTokenProvider GetTokenProvider()
        {
            if (accessTokenProviderComponent == null)
                return null;

            IAccessTokenProvider provider = accessTokenProviderComponent as IAccessTokenProvider;
            if (provider == null)
                throw new InvalidOperationException("The assigned component does not implement IAccessTokenProvider.");

            return provider;
        }

        private void ValidateEndpoint(bool authenticated)
        {
            if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out Uri endpoint) ||
                (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("The upload endpoint must be an absolute HTTP or HTTPS URL.");
            }

            if (authenticated && endpoint.Scheme != Uri.UriSchemeHttps && !endpoint.IsLoopback)
                throw new InvalidOperationException("Bearer token uploads require HTTPS except for localhost testing.");
        }
    }
}
