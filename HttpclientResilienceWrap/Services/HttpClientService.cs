using HttpclientResilienceWrap.Abstractions;
using HttpclientResilienceWrap.Extensions;
using HttpclientResilienceWrap.Options;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HttpclientResilienceWrap.Services
{
    /// <summary>
    /// http client service implementation
    /// </summary>
    /// <param name="_logger"></param>
    /// <param name="_httpClientFactory"></param>
    public sealed class HttpClientService(
        ILogger<HttpClientService> _logger,
        IHttpClientFactory _httpClientFactory,
        ExternalServiceConfig _serviceConfig) : IHttpClientService
    {
        private readonly JsonSerializerOptions _jsonOption = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Keyed by (service config type, response type) so each external service has its own
        // isolated circuit breaker state that persists across scoped service instances.
        private static readonly ConcurrentDictionary<(Type, Type), object> _circuitBreakerPipelines = new();

        /// <summary>
        /// GetAsync method to send a GET request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="HttpRequestParameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpApiResponse<T>> GetAsync<T>(HttpRequestParameter HttpRequestParameter, CancellationToken cancellationToken = default)
        {
            return await SendAsync<T>(HttpMethod.Get, HttpRequestParameter, cancellationToken);
        }

        /// <summary>
        /// PostAsync method to send a POST request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="HttpRequestParameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpApiResponse<T>> PostAsync<T>(HttpRequestParameter HttpRequestParameter, CancellationToken cancellationToken = default)
        {
            return await SendAsync<T>(HttpMethod.Post, HttpRequestParameter, cancellationToken);
        }


        /// <summary>
        /// PutAsync method to send a PUT request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="HttpRequestParameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpApiResponse<T>> PutAsync<T>(HttpRequestParameter HttpRequestParameter, CancellationToken cancellationToken = default)
        {
            return await SendAsync<T>(HttpMethod.Put, HttpRequestParameter, cancellationToken);
        }

        /// <summary>
        /// DeleteAsync method to send a DELETE request
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="HttpRequestParameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpApiResponse<T>> DeleteAsync<T>(HttpRequestParameter HttpRequestParameter, CancellationToken cancellationToken = default)
        {
            return await SendAsync<T>(HttpMethod.Delete, HttpRequestParameter, cancellationToken);
        }

        /// <summary>
        /// SendAsync method to send an HTTP request with the specified method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="method"></param>
        /// <param name="HttpRequestParameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Uri ResolveRequestUri(HttpRequestParameter HttpRequestParameter)
        {
            if (!string.IsNullOrEmpty(HttpRequestParameter.Path))
            {
                if (string.IsNullOrEmpty(_serviceConfig.BaseUri))
                    throw new InvalidOperationException(
                        $"HttpRequestParameter.Path is set to '{HttpRequestParameter.Path}' but ExternalServiceConfig.BaseUri is not configured.");

                var baseUri = _serviceConfig.BaseUri.TrimEnd('/');
                var path = HttpRequestParameter.Path.TrimStart('/');
                return new Uri(string.IsNullOrEmpty(path) ? baseUri : $"{baseUri}/{path}");
            }

            if (HttpRequestParameter.Uri is not null)
                return HttpRequestParameter.Uri;

            if (!string.IsNullOrEmpty(_serviceConfig.BaseUri))
                return new Uri(_serviceConfig.BaseUri);

            throw new InvalidOperationException(
                "No URI could be resolved for the request. Set HttpRequestParameter.Path, HttpRequestParameter.Uri, or ExternalServiceConfig.BaseUri.");
        }

        private async Task<HttpApiResponse<T>> SendAsync<T>(HttpMethod method, HttpRequestParameter HttpRequestParameter, CancellationToken cancellationToken)
        {
            Uri resolvedUri;
            try
            {
                resolvedUri = ResolveRequestUri(HttpRequestParameter);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Request configuration error: {Message}", ex.Message);
                return new()
                {
                    IsSuccessStatusCode = false,
                    StatusCode = HttpStatusCode.BadRequest,
                    ReasonPhrase = "Invalid Request Configuration",
                    ErrorMessage = ex.Message
                };
            }

            // Resolve once so all retry attempts share the same correlation ID.
            var correlationId = ResolveCorrelationId(HttpRequestParameter);

            _logger.LogInformation(
                "Sending request to {Uri} - Method: {Method} - CorrelationId: {CorrelationId}",
                resolvedUri, method.ToString(), correlationId ?? "none");

            var client = _httpClientFactory.CreateClient(_serviceConfig.ClientName);

            if (HttpRequestParameter.ClearHeaders)
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Clear();
            }

            // Pipeline order: Retry (outermost) -> Circuit Breaker -> Timeout (innermost, per attempt)
            var retryPipeline = BuildRetryPipeline<T>(method, resolvedUri, HttpRequestParameter);
            var circuitBreakerPipeline = GetOrBuildCircuitBreakerPipeline<T>();
            var timeoutPipeline = BuildTimeoutPipeline<T>(HttpRequestParameter);

            try
            {
                return await retryPipeline.ExecuteAsync(async outerToken =>
                    await circuitBreakerPipeline.ExecuteAsync(async cbToken =>
                        await timeoutPipeline.ExecuteAsync(
                            async innerToken => await BuildAndSendRequestAsync<T>(client, method, resolvedUri, HttpRequestParameter, correlationId, innerToken),
                            cbToken),
                    outerToken),
                cancellationToken);
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogWarning(
                    "Circuit breaker is open for {Uri} - skipping request. Reason: {Message}",
                    resolvedUri, ex.Message);

                return new HttpApiResponse<T>
                {
                    IsSuccessStatusCode = false,
                    StatusCode = HttpStatusCode.ServiceUnavailable,
                    ReasonPhrase = "Circuit Breaker Open",
                    ErrorMessage = ex.Message
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    "Network error after all retry attempts for {Uri}: {Message}",
                    resolvedUri, ex.Message);

                return new HttpApiResponse<T>
                {
                    IsSuccessStatusCode = false,
                    StatusCode = ex.StatusCode ?? HttpStatusCode.ServiceUnavailable,
                    ReasonPhrase = "Network Error",
                    ErrorMessage = ex.Message
                };
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Request timed out after all retry attempts for {Uri}: {Message}",
                    resolvedUri, ex.Message);

                return new HttpApiResponse<T>
                {
                    IsSuccessStatusCode = false,
                    StatusCode = HttpStatusCode.RequestTimeout,
                    ReasonPhrase = "Request Timeout",
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<HttpApiResponse<T>> BuildAndSendRequestAsync<T>(
            HttpClient client,
            HttpMethod method,
            Uri resolvedUri,
            HttpRequestParameter HttpRequestParameter,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            // Create new request for each attempt to avoid reuse issues
            var request = new HttpRequestMessage(method, resolvedUri);

            if (HttpRequestParameter.Body is not null)
                request.Content = new StringContent(
                    JsonSerializer.Serialize(HttpRequestParameter.Body, _jsonOption),
                    Encoding.UTF8,
                    HttpRequestParameter.MediaType ?? "application/json");

            if (HttpRequestParameter.Headers is not null)
                foreach (var header in HttpRequestParameter.Headers)
                    request.Headers.Add(header.Key, header.Value);

            if (!string.IsNullOrEmpty(HttpRequestParameter.Token) && !request.Headers.Contains("Authorization"))
                request.Headers.Add("Authorization", $"Bearer {HttpRequestParameter.Token}");

            if (correlationId is not null && !request.Headers.Contains(_serviceConfig.CorrelationId.HeaderName))
                request.Headers.TryAddWithoutValidation(_serviceConfig.CorrelationId.HeaderName, correlationId);

            var debugLogging = HttpRequestParameter.EnableDebugLogging || _serviceConfig.EnableDebugLogging;

            if (debugLogging)
                _logger.LogDebug("Curl: {Curl}", CreateCurl(request, HttpRequestParameter.Body));

            using var response = await client.SendAsync(request, cancellationToken);
            return await HandleApiResponse<T>(response, HttpRequestParameter);
        }

        private ResiliencePipeline<HttpApiResponse<T>> BuildRetryPipeline<T>(HttpMethod method, Uri resolvedUri, HttpRequestParameter HttpRequestParameter)
        {
            var maxRetries = HttpRequestParameter.Retry ?? _serviceConfig.Retry;
            var builder = new ResiliencePipelineBuilder<HttpApiResponse<T>>();

            // Polly requires MaxRetryAttempts >= 1; skip the strategy when retries are disabled.
            if (maxRetries > 0)
            {
                builder.AddRetry(new RetryStrategyOptions<HttpApiResponse<T>>
                {
                    MaxRetryAttempts = maxRetries,
                    Delay = TimeSpan.FromMilliseconds(_serviceConfig.RetryDelayMilliseconds),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder<HttpApiResponse<T>>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(response =>
                            response.StatusCode == HttpStatusCode.RequestTimeout ||
                            response.StatusCode == HttpStatusCode.TooManyRequests ||
                            (int)response.StatusCode >= 500),
                    OnRetry = args =>
                    {
                        _logger.LogWarning(
                            "Retry attempt {AttemptNumber} for {Method} {Uri}. Reason: {Outcome}",
                            args.AttemptNumber, method, resolvedUri,
                            args.Outcome.Exception?.Message ?? $"HTTP {(args.Outcome.Result?.StatusCode)}");
                        return ValueTask.CompletedTask;
                    }
                });
            }

            return builder.Build();
        }

        private ResiliencePipeline<HttpApiResponse<T>> BuildTimeoutPipeline<T>(HttpRequestParameter HttpRequestParameter)
        {
            return new ResiliencePipelineBuilder<HttpApiResponse<T>>()
                .AddTimeout(TimeSpan.FromSeconds(HttpRequestParameter.Timeout ?? _serviceConfig.TimeoutSeconds))
                .Build();
        }

        private ResiliencePipeline<HttpApiResponse<T>> GetOrBuildCircuitBreakerPipeline<T>()
        {
            var key = (_serviceConfig.GetType(), typeof(T));
            return (ResiliencePipeline<HttpApiResponse<T>>)_circuitBreakerPipelines.GetOrAdd(
                key,
                _ => BuildCircuitBreakerPipeline<T>()
            );
        }

        private ResiliencePipeline<HttpApiResponse<T>> BuildCircuitBreakerPipeline<T>()
        {
            var cb = _serviceConfig.CircuitBreaker;

            if (!cb.Enabled)
                return new ResiliencePipelineBuilder<HttpApiResponse<T>>().Build();

            return new ResiliencePipelineBuilder<HttpApiResponse<T>>()
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpApiResponse<T>>
                {
                    MinimumThroughput = cb.MinimumThroughput,
                    FailureRatio = cb.FailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(cb.SamplingDurationSeconds),
                    BreakDuration = TimeSpan.FromSeconds(cb.BreakDurationSeconds),
                    ShouldHandle = new PredicateBuilder<HttpApiResponse<T>>()
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .HandleResult(response =>
                            response.StatusCode == HttpStatusCode.RequestTimeout ||
                            response.StatusCode == HttpStatusCode.TooManyRequests ||
                            (int)response.StatusCode >= 500),
                    OnOpened = args =>
                    {
                        _logger.LogError(
                            "Circuit breaker OPENED for service {BaseUri}. Break duration: {BreakDuration}s. Reason: {Reason}",
                            _serviceConfig.BaseUri,
                            cb.BreakDurationSeconds,
                            args.Outcome.Exception?.Message ?? $"HTTP {(args.Outcome.Result?.StatusCode)}");
                        return ValueTask.CompletedTask;
                    },
                    OnClosed = args =>
                    {
                        _logger.LogInformation(
                            "Circuit breaker CLOSED for service {BaseUri}. Service recovered.",
                            _serviceConfig.BaseUri);
                        return ValueTask.CompletedTask;
                    },
                    OnHalfOpened = args =>
                    {
                        _logger.LogWarning(
                            "Circuit breaker HALF-OPEN for service {BaseUri}. Sending trial request.",
                            _serviceConfig.BaseUri);
                        return ValueTask.CompletedTask;
                    }
                })
                .Build();
        }

        private string? ResolveCorrelationId(HttpRequestParameter HttpRequestParameter)
        {
            var opts = _serviceConfig.CorrelationId;
            if (!opts.Enabled)
                return null;
            if (!string.IsNullOrEmpty(HttpRequestParameter.CorrelationId))
                return HttpRequestParameter.CorrelationId;
            return opts.GenerateIfAbsent ? Guid.NewGuid().ToString() : null;
        }

        /// <summary>
        /// HandleApiResponse method to process the HTTP response and return a typed response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <param name="HttpRequestParameter"></param>
        /// <returns></returns>
        private async Task<HttpApiResponse<T>> HandleApiResponse<T>(HttpResponseMessage response, HttpRequestParameter HttpRequestParameter)
        {
            _logger.LogInformation("Response - Success: {IsSuccessStatusCode} - StatusCode: {StatusCode} - ReasonPhrase - {ReasonPhrase}", response.IsSuccessStatusCode, response.StatusCode, response.ReasonPhrase);
            var apiResponse = new HttpApiResponse<T>
            {
                IsSuccessStatusCode = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                ReasonPhrase = response.ReasonPhrase
            };

            var content = await response.Content.ReadAsStringAsync();

            if (HttpRequestParameter.EnableDebugLogging || _serviceConfig.EnableDebugLogging)
            {
                var sanitizedContent = SanitizeForLogging(content);
                _logger.LogDebug("Response Content: {Content}", sanitizedContent);
            }
            else
            {
                _logger.LogInformation("Response received (length: {Length} chars, enable debug logging to see content)", content?.Length ?? 0);
            }

            if (content is not null && !string.IsNullOrEmpty(content))
            {
                apiResponse.Content = typeof(T) == typeof(string) ? (T)(object)content : JsonSerializer.Deserialize<T>(content, _jsonOption);
            }

            return apiResponse;
        }

        /// <summary>
        /// Sanitizes content for logging by redacting sensitive information and truncating if necessary
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static string SanitizeForLogging(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Truncate if too long
            if (content.Length > 1000)
                content = string.Concat(content.AsSpan(0, 1000), "... [truncated]");

            // Redact sensitive patterns (case-insensitive)
            content = Regex.Replace(content, "\"password\"\\s*:\\s*\".*?\"", "\"password\":\"***\"", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, "\"token\"\\s*:\\s*\".*?\"", "\"token\":\"***\"", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, "\"apikey\"\\s*:\\s*\".*?\"", "\"apikey\":\"***\"", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, "\"api_key\"\\s*:\\s*\".*?\"", "\"api_key\":\"***\"", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, "\"secret\"\\s*:\\s*\".*?\"", "\"secret\":\"***\"", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"""authorization""\s*:\s*""[^""]*""", @"""authorization"":""***""", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"""bearer""\s*:\s*""[^""]*""", @"""bearer"":""***""", RegexOptions.IgnoreCase);

            // Redact common sensitive field patterns
            content = Regex.Replace(content, @"""ssn""\s*:\s*""[^""]*""", @"""ssn"":""***""", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"""creditcard""\s*:\s*""[^""]*""", @"""creditcard"":""***""", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"""credit_card""\s*:\s*""[^""]*""", @"""credit_card"":""***""", RegexOptions.IgnoreCase);
            content = Regex.Replace(content, @"""email""\s*:\s*""[^""]*""", @"""email"":""***""", RegexOptions.IgnoreCase);

            return content;
        }

        /// <summary>
        /// CreateCurl method to generate a cURL command from the HttpRequestMessage
        /// </summary>
        /// <param name="request"></param>
        /// <param name="bodyObject"></param>
        /// <returns></returns>
        private string CreateCurl(HttpRequestMessage request, object? bodyObject)
        {
            var curl = new StringBuilder("curl");
            curl.Append($" -X {request.Method.Method} \"{request.RequestUri}\"");
            foreach (var header in request.Headers)
            {
                curl.Append($" -H \"{header.Key}: {string.Join(", ", header.Value)}\"");
            }
            if (bodyObject is not null)
            {
                var jsonBody = JsonSerializer.Serialize(bodyObject, _jsonOption);
                var sanitizedBody = SanitizeForLogging(jsonBody);
                curl.Append($" -d '{sanitizedBody}'");
            }
            return curl.ToString();
        }
    }
}
