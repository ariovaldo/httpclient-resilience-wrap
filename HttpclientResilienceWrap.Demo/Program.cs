using HttpclientResilienceWrap.Demo.Configs;
using HttpclientResilienceWrap.Demo.Endpoints;
using HttpclientResilienceWrap.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── External service configurations ──────────────────────────────────────────
var jpConfig    = builder.Configuration.GetSection("JsonPlaceholder").Get<JsonPlaceholderConfig>()!;
var flakyConfig = builder.Configuration.GetSection("Flaky").Get<FlakyConfig>()!;

// ── Resilient HttpClient registrations (one per external API) ─────────────────
builder.Services
    .InstallHttpClient(jpConfig)
    .InstallHttpClient(flakyConfig)
    .InstallHttpClient("IpInfo");           // no config needed — URI supplied per request

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
    o.SwaggerDoc("v1", new()
    {
        Title       = "HttpclientResilienceWrap — Demo API",
        Version     = "v1",
        Description = "Live demonstrations of retry, circuit breaker, timeout and correlation ID propagation."
    }));

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ── Route groups (one tag per section in Swagger) ─────────────────────────────
app.MapGroup("/demo").WithTags("JsonPlaceholder").MapGetEndpoints();
app.MapGroup("/demo").WithTags("IP Info").MapIpInfoEndpoints();
app.MapGroup("/demo").WithTags("Flow").MapRetryEndpoints();
app.MapGroup("/demo").WithTags("Flow").MapCircuitBreakerEndpoints();
app.MapGroup("/demo").WithTags("Flow").MapTimeoutEndpoints();
app.MapGroup("/demo").WithTags("Health").MapHealthEndpoints();

app.Run();
