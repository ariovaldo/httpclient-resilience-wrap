using HttpclientResilienceWrap.Options;

namespace HttpclientResilienceWrap.Demo.Configs;

/// <summary>
/// Configuration for httpstat.us — a service that returns any HTTP status code on demand.
/// Used to demonstrate retry backoff, circuit breaker trips, and timeout behaviour.
/// </summary>
public sealed class FlakyConfig : ExternalServiceConfig;
