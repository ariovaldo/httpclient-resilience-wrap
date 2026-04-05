namespace HttpclientResilienceWrap.Tests.TestHelpers;

/// <summary>Minimal DTO for JSON deserialization tests.</summary>
public sealed class MockHttpResponse
{
    public string Value { get; set; } = "info";
}
