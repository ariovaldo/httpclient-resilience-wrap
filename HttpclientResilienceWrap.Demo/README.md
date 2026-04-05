# HttpclientResilienceWrap — Demo API

Minimal API that demonstrates every feature of the [HttpclientResilienceWrap](../README.md) library using free, public APIs.

## Running

```shell
cd HttpclientResilienceWrap.Demo
dotnet run
```

Swagger UI opens automatically at **https://localhost:5001/swagger**.

## External services used

| Service | Base URI | Purpose |
|---------|----------|---------|
| [JSONPlaceholder](https://jsonplaceholder.typicode.com) | `https://jsonplaceholder.typicode.com` | Stable API for normal GET/POST flows |
| [httpstat.us](https://httpstat.us) | `https://httpstat.us` | Returns any HTTP status code on demand — forces retry, circuit breaker, timeout |
| [ipinfo.io](https://ipinfo.io) | `https://ipinfo.io` | Returns the server's public IP as plain text |

## Endpoints

### JsonPlaceholder

| Method | Route | What it demonstrates |
|--------|-------|----------------------|
| GET | `/demo/todos` | **Untyped** response — deserialized as `List<object>` |
| GET | `/demo/users` | **Typed** response — deserialized into `List<User>` with nested `Address`, `Geo`, `Company` records |
| POST | `/demo/post` | POST with a **caller-supplied correlation ID** (`X-Correlation-Id`) |

### IP Info

| Method | Route | What it demonstrates |
|--------|-------|----------------------|
| GET | `/demo/ip` | **Config-less registration** via `InstallHttpClient("IpInfo")` — URI supplied per request via `HttpRequestParameter.Uri` |

### Flow (resilience pipeline)

| Method | Route | What it demonstrates |
|--------|-------|----------------------|
| GET | `/demo/retry` | Calls `/503` — **retry with exponential backoff** (3 attempts, 300 ms base delay). Watch the console logs |
| GET | `/demo/circuit-breaker` | Calls `/500` repeatedly — after the failure threshold the **circuit opens** and returns synthetic 503 without network call |
| GET | `/demo/timeout` | Calls `/200?sleep=5000` with 2 s timeout — **per-attempt timeout** fires before the server responds |

### Health

| Method | Route | What it demonstrates |
|--------|-------|----------------------|
| GET | `/demo/health` | Returns the **active resilience configuration** for each registered service (no secrets) |

## Registration patterns demonstrated

### 1. Config class + appsettings.json (full control)

```csharp
var jpConfig = builder.Configuration.GetSection("JsonPlaceholder").Get<JsonPlaceholderConfig>()!;
builder.Services.InstallHttpClient(jpConfig);
```

Resolved via `[FromKeyedServices(nameof(JsonPlaceholderConfig))]`.

### 2. Config-less (defaults only, URI per request)

```csharp
builder.Services.InstallHttpClient("IpInfo");
```

Resolved via `[FromKeyedServices("IpInfo")]`. No config class, no appsettings section — the target URI is passed per request:

```csharp
await http.GetAsync<string>(new HttpRequestParameter
{
    Uri = new Uri("https://ipinfo.io/ip")
});
```

## Project structure

```
HttpclientResilienceWrap.Demo/
├── Program.cs                          # DI + route groups
├── appsettings.json                    # Service configurations
├── Properties/
│   └── launchSettings.json             # Opens Swagger on F5
├── Configs/
│   ├── JsonPlaceholderConfig.cs        # ExternalServiceConfig subclass
│   └── FlakyConfig.cs                  # ExternalServiceConfig subclass
├── Models/
│   ├── Post.cs                         # record Post(UserId, Id, Title, Body)
│   └── User.cs                         # record User(...) + Address, Geo, Company
└── Endpoints/
    ├── GetEndpoints.cs                 # /todos, /users, /post
    ├── IpInfoEndpoints.cs              # /ip
    ├── RetryEndpoints.cs               # /retry
    ├── CircuitBreakerEndpoints.cs       # /circuit-breaker
    ├── TimeoutEndpoints.cs             # /timeout
    └── HealthEndpoints.cs              # /health
```
