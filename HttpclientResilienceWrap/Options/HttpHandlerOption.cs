namespace HttpclientResilienceWrap.Options
{
    /// <summary>
    /// Configuration options for the underlying <see cref="System.Net.Http.HttpClientHandler"/>
    /// and its lifetime within the <see cref="System.Net.Http.IHttpClientFactory"/>.
    /// </summary>
    public class HttpHandlerOption
    {
        /// <summary>
        /// How long the handler (and its connections) is kept alive before being recycled,
        /// allowing DNS changes to propagate. Defaults to 5 minutes.
        /// </summary>
        public int LifetimeMinutes { get; set; } = 5;

        /// <summary>
        /// Maximum number of concurrent connections allowed to the same server endpoint.
        /// Defaults to 20.
        /// </summary>
        public int MaxConnectionsPerServer { get; set; } = 20;

        /// <summary>
        /// Whether the handler should automatically follow HTTP redirect responses.
        /// Defaults to true.
        /// </summary>
        public bool AllowAutoRedirect { get; set; } = true;

        /// <summary>
        /// Maximum number of automatic redirections to follow. Only applies when
        /// <see cref="AllowAutoRedirect"/> is true. Defaults to 3.
        /// </summary>
        public int MaxAutomaticRedirections { get; set; } = 3;
    }
}
