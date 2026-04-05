using HttpclientResilienceWrap.Abstractions;
using HttpclientResilienceWrap.Demo.Configs;
using HttpclientResilienceWrap.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace HttpclientResilienceWrap.Demo.Endpoints;

/// <summary>
/// Demonstrates exponential backoff retry.
/// httpstat.us/503 always returns 503 Service Unavailable, forcing the retry pipeline
/// to exhaust all attempts before returning the final failure response.
/// Watch the console logs to see each attempt and the backoff delays.
/// </summary>
public static class RetryEndpoints
{
    public static RouteGroupBuilder MapRetryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/retry", Handle)
            .WithName("RetryDemo")
            .WithSummary("Retry backoff")
            .WithDescription(
                "Calls httpstat.us/503 which always fails. " +
                "The library retries 3 times with exponential backoff (300 ms → 600 ms → 1 200 ms). " +
                "Observe each attempt in the console logs.");

        return group;
    }

    private static async Task<IResult> Handle(
        [FromKeyedServices(nameof(FlakyConfig))] IHttpClientService http,
        CancellationToken ct)
    {
        var response = await http.GetAsync<object>(
            new HttpRequestParameter
            {
                Path  = "503",
                Retry = 3
            },
            ct);

        return Results.Ok(new
        {
            hint     = "Check the console — you should see 3 retry attempts with increasing delays.",
            response
        });
    }
}
