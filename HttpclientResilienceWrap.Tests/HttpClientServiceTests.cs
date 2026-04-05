using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HttpclientResilienceWrap.Extensions;
using HttpclientResilienceWrap.Services;
using HttpclientResilienceWrap.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HttpclientResilienceWrap.Tests;

public sealed class HttpClientServiceTests
{
    private static HttpClientService CreateSut(TestServiceConfig config, MockHttpMessageHandler handler, out Mock<IHttpClientFactory> factoryMock)
    {
        var client = new HttpClient(handler);
        factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(config.ClientName)).Returns(client);
        return new HttpClientService(NullLogger<HttpClientService>.Instance, factoryMock.Object, config);
    }

    [Fact]
    public async Task GetAsync_success_deserializes_json_body()
    {
        var config = new TestServiceConfig { Retry = 0 };
        var handler = new MockHttpMessageHandler();
        var json = JsonSerializer.Serialize(new MockHttpResponse { Value = "unit" });
        handler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var sut = CreateSut(config, handler, out _);
        var result = await sut.GetAsync<MockHttpResponse>(new HttpRequestParameter { Path = "items/1" });

        result.IsSuccessStatusCode.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Should().NotBeNull();
        result.Content!.Value.Should().Be("unit");
        result.ErrorMessage.Should().BeNull();
        handler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task GetAsync_when_path_without_base_uri_returns_bad_request()
    {
        var config = new TestServiceConfig { BaseUri = "", Retry = 0 };
        var handler = new MockHttpMessageHandler();
        var sut = CreateSut(config, handler, out _);

        var result = await sut.GetAsync<MockHttpResponse>(new HttpRequestParameter { Path = "x" });

        result.IsSuccessStatusCode.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.ReasonPhrase.Should().Be("Invalid Request Configuration");
        result.ErrorMessage.Should().Contain("BaseUri");
        handler.RequestCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_retries_on_500_then_succeeds()
    {
        var config = new TestServiceConfig { Retry = 2, RetryDelayMilliseconds = 0 };
        var handler = new MockHttpMessageHandler();
        var json = JsonSerializer.Serialize(new MockHttpResponse { Value = "recovered" });
        var attempts = 0;
        handler.SetDynamicResponse(() =>
        {
            attempts++;
            if (attempts < 3)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var sut = CreateSut(config, handler, out _);
        var result = await sut.GetAsync<MockHttpResponse>(new HttpRequestParameter { Path = "flaky" });

        result.IsSuccessStatusCode.Should().BeTrue();
        result.Content!.Value.Should().Be("recovered");
        handler.RequestCount.Should().Be(3);
    }

    [Fact]
    public async Task PostAsync_sends_json_body()
    {
        var config = new TestServiceConfig { Retry = 0 };
        var handler = new MockHttpMessageHandler();
        handler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        var sut = CreateSut(config, handler, out _);
        await sut.PostAsync<MockHttpResponse>(
            new HttpRequestParameter
            {
                Path = "orders",
                Body = new { OrderId = 42 }
            });

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequestContent.Should().Contain("OrderId").And.Contain("42");
    }

    [Fact]
    public async Task GetAsync_sends_bearer_token_header()
    {
        var config = new TestServiceConfig { Retry = 0 };
        var handler = new MockHttpMessageHandler();
        handler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        var sut = CreateSut(config, handler, out _);
        await sut.GetAsync<MockHttpResponse>(new HttpRequestParameter { Path = "x", Token = "secret-token" });

        handler.LastRequest!.Headers.Authorization.Should().NotBeNull();
        handler.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest.Headers.Authorization.Parameter.Should().Be("secret-token");
    }

    [Fact]
    public async Task CreateClient_is_called_with_config_client_name()
    {
        var config = new TestServiceConfig { Retry = 0 };
        var handler = new MockHttpMessageHandler();
        handler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        var sut = CreateSut(config, handler, out var factoryMock);
        await sut.GetAsync<MockHttpResponse>(new HttpRequestParameter { Path = "x" });

        factoryMock.Verify(f => f.CreateClient(config.ClientName), Times.Once);
    }
}
