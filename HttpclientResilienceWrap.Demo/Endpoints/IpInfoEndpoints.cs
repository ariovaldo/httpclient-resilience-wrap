using HttpclientResilienceWrap.Abstractions;
using HttpclientResilienceWrap.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace HttpclientResilienceWrap.Demo.Endpoints;

/// <summary>
/// Demonstrates a GET call using InstallHttpClient("IpInfo") — no config class,
/// no appsettings section. The full URI is supplied per request via HttpRequestParameter.Uri.
/// ipinfo.io/ip returns the server's outbound public IP address as raw text.
/// </summary>
public static class IpInfoEndpoints
{
    public static RouteGroupBuilder MapIpInfoEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/ip", Handle)
            .WithName("GetPublicIp")
            .WithSummary("Public IP (no config, URI per request)")
            .WithDescription(
                "Calls https://ipinfo.io/ip using the config-less InstallHttpClient(\"IpInfo\") overload. " +
                "The target URI is passed via HttpRequestParameter.Uri — no BaseUri, no config class, no appsettings section.");

        return group;
    }

    private static async Task<IResult> Handle(
        [FromKeyedServices("IpInfo")] IHttpClientService http,
        CancellationToken ct)
    {
        var response = await http.GetAsync<string>(
            new HttpRequestParameter
            {
                Uri = new Uri("https://ipinfo.io/ip")
            },
            ct);

        return Results.Ok(response);
    }
}
