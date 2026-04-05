using HttpclientResilienceWrap.Abstractions;
using HttpclientResilienceWrap.Options;
using HttpclientResilienceWrap.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HttpclientResilienceWrap.Extensions
{
    /// <summary>
    /// Extension methods on <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>
    /// for registering resilient <see cref="System.Net.Http.HttpClient"/> instances backed by Polly pipelines.
    /// </summary>
    public static class HttpClientExtension
    {
        /// <summary>
        /// Registers a named <see cref="System.Net.Http.HttpClient"/> and a keyed
        /// <see cref="HttpClientService"/> scoped to <typeparamref name="TConfig"/>.
        /// Call once per external service; each registration is fully isolated —
        /// separate connection pool, BaseAddress, and resilience pipeline.
        /// <para>
        /// Resolve via <c>[FromKeyedServices(nameof(MyApiConfig))] IHttpClientService</c>
        /// or <c>sp.GetRequiredKeyedService&lt;IHttpClientService&gt;(nameof(MyApiConfig))</c>.
        /// </para>
        /// </summary>
        public static IServiceCollection InstallHttpClient<TConfig>(
            this IServiceCollection services,
            TConfig config) where TConfig : ExternalServiceConfig
        {
            services.AddHttpClient(config.ClientName, client =>
            {
                if (!string.IsNullOrEmpty(config.BaseUri))
                    client.BaseAddress = new Uri(config.BaseUri.TrimEnd('/') + "/");
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(config.Handler.LifetimeMinutes))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = config.Handler.MaxConnectionsPerServer,
                AllowAutoRedirect = config.Handler.AllowAutoRedirect,
                MaxAutomaticRedirections = config.Handler.MaxAutomaticRedirections
            });

            // Keyed registration — each config type gets its own IHttpClientService instance,
            // preventing one service's config from leaking into another.
            services.AddKeyedScoped<IHttpClientService>(config.ClientName, (sp, _) =>
                new HttpClientService(
                    sp.GetRequiredService<ILogger<HttpClientService>>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    config));

            return services;
        }

        /// <summary>
        /// Registers a named <see cref="System.Net.Http.HttpClient"/> and a keyed
        /// <see cref="HttpClientService"/> using only default resilience settings
        /// (no BaseUri, no ApiKey). Callers supply the target URI per request via
        /// <see cref="HttpRequestParameter.Uri"/>.
        /// <para>
        /// Resolve via <c>[FromKeyedServices("clientName")] IHttpClientService</c>.
        /// </para>
        /// </summary>
        public static IServiceCollection InstallHttpClient(
            this IServiceCollection services,
            string clientName)
        {
            var config = new DefaultServiceConfig { ClientName = clientName };
            return services.InstallHttpClient(config);
        }

        private sealed class DefaultServiceConfig : ExternalServiceConfig;
    }
}
