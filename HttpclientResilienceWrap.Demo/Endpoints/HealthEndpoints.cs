namespace HttpclientResilienceWrap.Demo.Endpoints;

/// <summary>
/// Returns the active resilience configuration for each registered service.
/// Useful for quickly verifying what was loaded from appsettings.json — no secrets exposed.
/// </summary>
public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/health", Handle)
            .WithName("Health")
            .WithSummary("Active configuration")
            .WithDescription(
                "Returns the active resilience settings for each registered service " +
                "(no secrets — ApiKey and ApiPass are omitted).");

        return group;
    }

    private static IResult Handle(IConfiguration config)
    {
        static object Summarise(IConfigurationSection s) => new
        {
            clientName         = s["ClientName"],
            baseUri            = s["BaseUri"],
            retry              = s["Retry"],
            retryDelayMs       = s["RetryDelayMilliseconds"],
            timeoutSeconds     = s["TimeoutSeconds"],
            circuitBreaker = new
            {
                failureRatio          = s["CircuitBreaker:FailureRatio"],
                minimumThroughput     = s["CircuitBreaker:MinimumThroughput"],
                samplingDurationSecs  = s["CircuitBreaker:SamplingDurationSeconds"],
                breakDurationSecs     = s["CircuitBreaker:BreakDurationSeconds"]
            }
        };

        return Results.Ok(new
        {
            status   = "healthy",
            services = new[]
            {
                Summarise(config.GetSection("JsonPlaceholder")),
                Summarise(config.GetSection("Flaky"))
            }
        });
    }
}
