using System.Net;

namespace HttpclientResilienceWrap.Tests.TestHelpers;

/// <summary>
/// Test double for <see cref="HttpMessageHandler"/> — inspects requests and returns fixed or dynamic responses (no real network).
/// </summary>
public sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private HttpResponseMessage? _fixedResponse;
    private Func<HttpResponseMessage>? _dynamicResponse;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestContent { get; private set; }
    public int RequestCount { get; private set; }

    public void SetResponse(HttpResponseMessage response)
    {
        _fixedResponse = response;
        _dynamicResponse = null;
    }

    public void SetDynamicResponse(Func<HttpResponseMessage> factory)
    {
        _dynamicResponse = factory;
        _fixedResponse = null;
    }

    public void Reset()
    {
        RequestCount = 0;
        LastRequest = null;
        LastRequestContent = null;
        _fixedResponse = null;
        _dynamicResponse = null;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Read content before invoking the factory so the factory can inspect captured state.
        LastRequest = request;
        LastRequestContent = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : null;
        RequestCount++;

        if (_dynamicResponse is not null)
            return _dynamicResponse();
        return _fixedResponse ?? new HttpResponseMessage(HttpStatusCode.OK);
    }
}
