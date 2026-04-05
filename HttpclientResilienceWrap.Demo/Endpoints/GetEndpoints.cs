using HttpclientResilienceWrap.Abstractions;
using HttpclientResilienceWrap.Demo.Configs;
using HttpclientResilienceWrap.Demo.Models;
using HttpclientResilienceWrap.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace HttpclientResilienceWrap.Demo.Endpoints;

/// <summary>
/// Normal GET and POST requests against JSONPlaceholder.
/// Demonstrates untyped (object) vs strongly-typed deserialization,
/// and a POST with caller-supplied correlation ID.
/// </summary>
public static class GetEndpoints
{
    public static RouteGroupBuilder MapGetEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/todos", HandleGetUntyped)
            .WithName("GetTodosUntyped")
            .WithSummary("GET /todos (untyped - object)")
            .WithDescription(
                "Fetches /todos from JSONPlaceholder deserialized as List<object>. " +
                "Good for quick prototyping when you don't have a model yet. " +
                "Compare the Content shape with the /users typed endpoint.");

        group.MapGet("/users", HandleGetTyped)
            .WithName("GetUsersTyped")
            .Produces<HttpApiResponse<List<User>>>()
            .WithSummary("GET /users (typed - User model)")
            .WithDescription(
                "Fetches /users from JSONPlaceholder deserialized into a strongly-typed List<User> " +
                "with nested Address, Geo and Company records. " +
                "Compare the Content shape with the /todos untyped endpoint.");

        group.MapPost("/post", HandlePost)
            .WithName("CreatePost")
            .WithSummary("POST with custom correlation ID")
            .WithDescription(
                "POSTs a new resource to JSONPlaceholder propagating a caller-supplied correlation ID. " +
                "Inspect the X-Correlation-Id header in the response and in the console logs.");

        return group;
    }

    private static async Task<IResult> HandleGetUntyped(
        [FromKeyedServices(nameof(JsonPlaceholderConfig))] IHttpClientService http,
        CancellationToken ct)
    {
        var response = await http.GetAsync<List<object>>(
            new HttpRequestParameter { Path = "todos" },
            ct);

        return Results.Ok(response);
    }

    private static async Task<IResult> HandleGetTyped(
        [FromKeyedServices(nameof(JsonPlaceholderConfig))] IHttpClientService http,
        CancellationToken ct)
    {
        var response = await http.GetAsync<List<User>>(
            new HttpRequestParameter { Path = "users" },
            ct);

        return Results.Ok(response);
    }

    private static async Task<IResult> HandlePost(
        [FromKeyedServices(nameof(JsonPlaceholderConfig))] IHttpClientService http,
        CancellationToken ct)
    {
        var correlationId = $"demo-{Guid.NewGuid():N}";

        var response = await http.PostAsync<Post>(
            new HttpRequestParameter
            {
                Path           = "posts",
                Body           = new { title = "Resilience Demo", body = "Testing HttpclientResilienceWrap", userId = 1 },
                CorrelationId  = correlationId
            },
            ct);

        return Results.Ok(new { correlationId, response });
    }
}
