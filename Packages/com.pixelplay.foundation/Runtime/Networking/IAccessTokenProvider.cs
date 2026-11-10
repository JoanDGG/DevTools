using System.Threading;
using System.Threading.Tasks;

namespace PixelPlay.Foundation.Networking
{
    /// <summary>Supplies the current access token for authenticated HTTP requests.</summary>
    public interface IAccessTokenProvider
    {
        /// <summary>Gets an access token, refreshing or obtaining it as needed by the implementation.</summary>
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    }
}
