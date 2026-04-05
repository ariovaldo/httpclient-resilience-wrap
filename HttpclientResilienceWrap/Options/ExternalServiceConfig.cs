namespace HttpclientResilienceWrap.Options
{
    /// <summary>
    /// External service configuration
    /// </summary>
    public abstract class ExternalServiceConfig
    {
        private string? _clientName;

        /// <summary>
        /// Name used to register and resolve the named <see cref="System.Net.Http.HttpClient"/> for this service.
        /// Defaults to the concrete type name (e.g. <c>PaymentApiConfig</c>), keeping each service isolated.
        /// Can be overridden via <c>appsettings.json</c> or by setting this property directly.
        /// </summary>
        public string ClientName
        {
            get => _clientName ?? GetType().Name;
            set => _clientName = value;
        }

        /// <summary>
        /// Base uri
        /// </summary>
        public string BaseUri { get; set; } = default!;

        /// <summary>
        /// Api Key
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string? ApiPass { get; set; }

        /// <summary>
        /// Default number of retries for all requests to this service. Defaults to 0 (no retries).
        /// Set to a positive value to enable exponential back-off retries.
        /// Can be overridden per request via <see cref="HttpclientResilienceWrap.Models.HttpRequestParameters.Retry"/>.
        /// </summary>
        public int Retry { get; set; } = 0;

        /// <summary>
        /// Base delay in milliseconds between retry attempts. Uses exponential back-off starting from this value.
        /// With 3 retries the total wait is ~3.5s (500ms -> 1000ms -> 2000ms). Set to 0 in test environments.
        /// </summary>
        public int RetryDelayMilliseconds { get; set; } = 500;

        /// <summary>
        /// Timeout
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// HTTP handler options controlling connection pooling, redirects, and handler lifetime.
        /// </summary>
        public HttpHandlerOption Handler { get; set; } = new();

        /// <summary>
        /// Circuit breaker options. When enabled, trips the circuit after a failure threshold
        /// is reached, preventing requests from being sent until the service recovers.
        /// </summary>
        public CircuitBreakerOption CircuitBreaker { get; set; } = new();

        /// <summary>
        /// Correlation ID options. When enabled, attaches a correlation ID header to every
        /// outbound request for distributed tracing. Can be overridden per request via
        /// <see cref="HttpclientResilienceWrap.Models.HttpRequestParameters.CorrelationId"/>.
        /// </summary>
        public CorrelationIdOption CorrelationId { get; set; } = new();

        //todo: add bulkhead options
        //todo: add timeout options
        //todo: add fallback options
        //todo: add cache options
        //todo: add logging options
        //todo: add metrics options
        //todo: add custom policies options
        //todo: add custom handlers options
    }
}
