using HttpclientResilienceWrap.Options;

namespace HttpclientResilienceWrap.Tests.TestHelpers;

/// <summary>
/// Concrete <see cref="ExternalServiceConfig"/> for unit tests: circuit breaker and correlation ID off, zero retry delay.
/// </summary>
public sealed class TestServiceConfig : ExternalServiceConfig
{
    public TestServiceConfig()
    {
        BaseUri = "http://httpclient-resilience-wrap.test";
        Retry = 3;
        TimeoutSeconds = 30;
        RetryDelayMilliseconds = 0;
        CircuitBreaker = new CircuitBreakerOption { Enabled = false };
        CorrelationId = new CorrelationIdOption { Enabled = false };
    }
}
