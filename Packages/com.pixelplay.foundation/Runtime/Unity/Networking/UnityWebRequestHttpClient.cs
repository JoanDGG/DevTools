using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PixelPlay.Foundation.Networking;
using UnityEngine.Networking;

namespace PixelPlay.Foundation.Unity.Networking
{
    /// <summary>Implements the HTTP client contract with UnityWebRequest.</summary>
    public sealed class UnityWebRequestHttpClient : IHttpClient
    {
        /// <inheritdoc />
        public async Task<HttpResponseData> SendAsync(
            HttpRequestData requestData,
            CancellationToken cancellationToken = default)
        {
            if (requestData == null)
                throw new ArgumentNullException(nameof(requestData));

            cancellationToken.ThrowIfCancellationRequested();

            using (var request = new UnityWebRequest(requestData.Url, requestData.Method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();

                byte[] body = requestData.Body;
                if (body.Length > 0)
                    request.uploadHandler = new UploadHandlerRaw(body);

                request.timeout = requestData.TimeoutSeconds;

                foreach (KeyValuePair<string, string> header in requestData.Headers)
                    request.SetRequestHeader(header.Key, header.Value);

                UnityWebRequestAsyncOperation operation;
                try
                {
                    operation = request.SendWebRequest();
                }
                catch (Exception exception)
                {
                    throw new HttpTransportException(requestData.Url, exception.Message, exception);
                }

                var completion = new TaskCompletionSource<bool>();
                operation.completed += _ => completion.TrySetResult(true);

                using (cancellationToken.Register(() => request.Abort()))
                    await completion.Task;

                cancellationToken.ThrowIfCancellationRequested();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    throw new HttpTransportException(requestData.Url, request.error);
                }

                Dictionary<string, string> responseHeaders = request.GetResponseHeaders();
                return new HttpResponseData(request.responseCode > 0 ? (int)request.responseCode : 0,
                    request.downloadHandler.data, responseHeaders);
            }
        }
    }
}
