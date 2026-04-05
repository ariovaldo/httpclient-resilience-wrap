namespace HttpclientResilienceWrap.Extensions
{
    /// <summary>
    /// HttpRequestParameter
    /// </summary>
    public sealed class HttpRequestParameter
    {
        /// <summary>
        /// Relative path appended to the <c>BaseUri</c> configured in <see cref="HttpclientResilienceWrap.Options.ExternalServiceConfig"/>.
        /// Use this as the primary way to specify the endpoint, e.g. <c>/users/123</c>.
        /// Mutually exclusive with <see cref="Uri"/>; when both are set, <see cref="Path"/> takes precedence.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Absolute URI override. Use only when targeting a host different from the configured <c>BaseUri</c>,
        /// e.g. a cross-service call. For normal requests prefer <see cref="Path"/>.
        /// </summary>
        public Uri? Uri { get; set; }

        /// <summary>
        /// Token for authentication
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Headers for the request
        /// </summary>
        public Dictionary<string, string>? Headers { get; set; }

        /// <summary>
        /// Body for the request
        /// </summary>
        public object? Body { get; set; }

        /// <summary>
        /// Media type (if was different from application/json)
        /// </summary>
        public string? MediaType { get; set; }

        /// <summary>
        /// Optional clean headers
        /// </summary>
        public bool ClearHeaders { get; set; } = false;

        /// <summary>
        /// Time out in seconds
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// Per-request retry override. When set, takes precedence over
        /// <see cref="HttpclientResilienceWrap.Options.ExternalServiceConfig.Retry"/>.
        /// </summary>
        public int? Retry { get; set; }

        /// <summary>
        /// Per-request correlation ID. When set, takes precedence over the auto-generated value
        /// controlled by <see cref="HttpclientResilienceWrap.Options.CorrelationIdOption.GenerateIfAbsent"/>.
        /// Use this to propagate an existing correlation ID received from an upstream caller.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Enable debug logging for request/response content
        /// </summary>
        public bool EnableDebugLogging { get; set; } = false;
    }
}
