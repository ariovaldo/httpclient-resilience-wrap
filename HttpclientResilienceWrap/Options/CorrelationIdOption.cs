namespace HttpclientResilienceWrap.Options
{
    /// <summary>
    /// Configuration options for attaching a correlation ID header to outbound HTTP requests,
    /// enabling distributed tracing across service boundaries.
    /// </summary>
    public class CorrelationIdOption
    {
        /// <summary>
        /// Enables or disables correlation ID propagation. Defaults to true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Name of the HTTP header used to carry the correlation ID.
        /// Defaults to <c>X-Correlation-Id</c>.
        /// </summary>
        public string HeaderName { get; set; } = "X-Correlation-Id";

        /// <summary>
        /// When true and no correlation ID is provided per request via
        /// <see cref="HttpclientResilienceWrap.Extensions.HttpRequestParameter.CorrelationId"/>,
        /// a new <see cref="System.Guid"/> is generated automatically. Defaults to true.
        /// </summary>
        public bool GenerateIfAbsent { get; set; } = true;
    }
}
