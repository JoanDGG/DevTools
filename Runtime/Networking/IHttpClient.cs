using System.Threading;
using System.Threading.Tasks;

namespace PixelPlay.Foundation.Networking
{
    /// <summary>Sends HTTP requests without exposing a platform-specific transport.</summary>
    public interface IHttpClient
    {
        /// <summary>
        /// Sends a request. HTTP status codes, including 4xx and 5xx, are returned as
        /// responses; transport failures throw <see cref="HttpTransportException"/>.
        /// </summary>
        Task<HttpResponseData> SendAsync(HttpRequestData request, CancellationToken cancellationToken = default);
    }
}
