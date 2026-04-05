namespace HttpclientResilienceWrap.Demo.Models;

/// <summary>
/// Typed model matching the JSONPlaceholder /users resource (nested objects included).
/// </summary>
public sealed record User(
    int Id,
    string Name,
    string Username,
    string Email,
    Address Address,
    string Phone,
    string Website,
    Company Company);

public sealed record Address(
    string Street,
    string Suite,
    string City,
    string Zipcode,
    Geo Geo);

public sealed record Geo(string Lat, string Lng);

public sealed record Company(string Name, string CatchPhrase, string Bs);
