using HttpclientResilienceWrap.Abstractions;
using HttpclientResilienceWrap.Demo.Configs;
using HttpclientResilienceWrap.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace HttpclientResilienceWrap.Demo.Endpoints;

/// <summary>
/// Demonstrates per-attempt timeout.
/// httpstat.us/200?sleep=5000 intentionally delays 5 seconds before responding.
/// The request override sets Timeout = 2 s, so the library cancels the attempt
/// after 2 seconds and returns a synthetic 408 with an ErrorMessage.
/// </summary>
public static class TimeoutEndpoints
{
    public static RouteGroupBuilder MapTimeoutEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/timeout", Handle)
            .WithName("TimeoutDemo")
            .WithSummary("Per-attempt timeout")
            .WithDescription(
                "Calls httpstat.us/200?sleep=5000 (5-second server delay) " +
                "with a 2-second per-attempt timeout override. " +
                "The library cancels after 2 s and returns a synthetic 408 with an ErrorMessage.");

        return group;
    }

    private static async Task<IResult> Handle(
        [FromKeyedServices(nameof(FlakyConfig))] IHttpClientService http,
        CancellationToken ct)
    {
        var response = await http.GetAsync<object>(
            new HttpRequestParameter
            {
                Path    = "200?sleep=5000",
                Timeout = 2
            },
            ct);

        return Results.Ok(new
        {
            hint     = "Notice StatusCode 408 and a non-null ErrorMessage — the request was cancelled before the server replied.",
            response
        });
    }
}
