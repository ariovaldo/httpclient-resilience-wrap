using HttpclientResilienceWrap.Extensions;

namespace HttpclientResilienceWrap.Abstractions
{
    /// <summary>
    /// HttpClient service interface
    /// </summary>
    public interface IHttpClientService
    {
        /// <summary>
        /// GetAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="HttpRequestParameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HttpApiResponse<T>> GetAsync<T>(HttpRequestParameter HttpRequestParameter, CancellationToken cancellationToken = default);

        /// <summary>
        /// PostAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="HttpRequestParameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HttpApiResponse<T>> PostAsync<T>(HttpRequestParameter HttpRequestParameter, CancellationToken cancellationToken = default);

        /// <summary>
        /// PutAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="HttpRequestParameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HttpApiResponse<T>> PutAsync<T>(HttpRequestParameter HttpRequestParameter, CancellationToken cancellationToken = default);

        /// <summary>
        /// DeleteAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="HttpRequestParameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HttpApiResponse<T>> DeleteAsync<T>(HttpRequestParameter HttpRequestParameter, CancellationToken cancellationToken = default);
    }
}
