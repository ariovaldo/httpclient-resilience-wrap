namespace HttpclientResilienceWrap.Options
{
    /// <summary>
    /// Configuration options for the circuit breaker resilience strategy.
    /// </summary>
    public class CircuitBreakerOption
    {
        /// <summary>
        /// Enables or disables the circuit breaker. Defaults to true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Minimum number of requests within the sampling window required before
        /// the circuit breaker can evaluate the failure ratio. Defaults to 10.
        /// </summary>
        public int MinimumThroughput { get; set; } = 10;

        /// <summary>
        /// Ratio of failures (0.0 to 1.0) within the sampling window that triggers
        /// the circuit breaker. Defaults to 0.5 (50%).
        /// </summary>
        public double FailureRatio { get; set; } = 0.5;

        /// <summary>
        /// Duration in seconds of the sampling window used to calculate the failure ratio.
        /// Defaults to 30 seconds.
        /// </summary>
        public int SamplingDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Duration in seconds the circuit remains open before transitioning to half-open
        /// and allowing a single trial request through. Defaults to 30 seconds.
        /// </summary>
        public int BreakDurationSeconds { get; set; } = 30;
    }
}
