using HttpclientResilienceWrap.Options;

namespace HttpclientResilienceWrap.Demo.Configs;

/// <summary>
/// Configuration for JSONPlaceholder (https://jsonplaceholder.typicode.com).
/// Used to demonstrate normal GET/POST flows with retry and circuit breaker.
/// </summary>
public sealed class JsonPlaceholderConfig : ExternalServiceConfig;
