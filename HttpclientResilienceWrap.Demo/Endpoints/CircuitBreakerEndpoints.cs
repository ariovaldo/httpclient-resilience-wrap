using HttpclientResilienceWrap.Abstractions;
using HttpclientResilienceWrap.Demo.Configs;
using HttpclientResilienceWrap.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace HttpclientResilienceWrap.Demo.Endpoints;

/// <summary>
/// Demonstrates the circuit breaker pattern.
/// httpstat.us/500 always returns 500 Internal Server Error.
/// After MinimumThroughput (3) requests within SamplingDurationSeconds (20 s)
/// with FailureRatio >= 50 %, the circuit opens.
/// While open, the library returns a synthetic 503 instantly — no network call is made.
/// After BreakDurationSeconds (10 s) the circuit transitions to half-open.
/// </summary>
public static class CircuitBreakerEndpoints
{
    public static RouteGroupBuilder MapCircuitBreakerEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/circuit-breaker", Handle)
            .WithName("CircuitBreakerDemo")
            .WithSummary("Circuit breaker")
            .WithDescription(
                "Calls httpstat.us/500 repeatedly. " +
                "After the failure threshold is reached the circuit opens: " +
                "subsequent calls return a synthetic 503 without hitting the network. " +
                "The circuit resets after 10 seconds.");

        return group;
    }

    private static async Task<IResult> Handle(
        [FromKeyedServices(nameof(FlakyConfig))] IHttpClientService http,
        CancellationToken ct)
    {
        var response = await http.GetAsync<object>(
            new HttpRequestParameter { Path = "500" },
            ct);

        return Results.Ok(new
        {
            hint =
                "Keep calling this endpoint. Once the circuit opens you will see StatusCode 503 " +
                "and an ErrorMessage — no actual HTTP request is sent to the server.",
            response
        });
    }
}
