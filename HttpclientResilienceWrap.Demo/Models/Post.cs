namespace HttpclientResilienceWrap.Demo.Models;

/// <summary>
/// Typed model matching the JSONPlaceholder /posts resource.
/// </summary>
public sealed record Post(int UserId, int Id, string Title, string Body);
