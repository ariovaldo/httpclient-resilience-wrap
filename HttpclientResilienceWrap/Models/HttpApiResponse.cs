using System.Net;

namespace HttpclientResilienceWrap.Extensions
{
    /// <summary>
    /// Api response generic class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class HttpApiResponse<T> 
    {
        /// <summary>
        /// Is Success
        /// </summary>
        public bool IsSuccessStatusCode { get; set; }

        /// <summary>
        /// Status code
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// ReasonPhrase
        /// </summary>
        public string? ReasonPhrase { get; set; }

        /// <summary>
        /// Content of the response
        /// </summary>
        public T? Content { get; set; }

        /// <summary>
        /// Set when the request could not be completed due to a network or infrastructure error
        /// (e.g. connection refused, circuit breaker open, timeout after all retries).
        /// <c>null</c> on successful requests and normal HTTP error responses.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
