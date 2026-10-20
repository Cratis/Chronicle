// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for configuring <see cref="IWebhookHttpClientFactory"/> <see cref="HttpClient"/>.
/// </summary>
public static class WebhookHttpClientExtensions
{
    /// <summary>
    /// Adds a <see cref="HttpClient"/> for <see cref="IWebhookHttpClientFactory"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ISiloBuilder"/>.</param>
    /// <returns>The builder for continuation.</returns>
    public static ISiloBuilder AddWebhookObserverHttpClient(this ISiloBuilder builder)
    {
        builder.Services.TryAddSingleton<IWebhookHostResolver, WebhookHostResolver>();
        builder.Services.TryAddSingleton<IWebhookTargetPolicy, WebhookTargetPolicy>();
        builder.Services.TryAddSingleton<WebhookConnection>();
        builder.Services.AddTransient<WebhookValidationHandler>();
        builder.Services.AddHostedService<WebhookValidationStartup>();
        builder.Services.AddHttpClient(OAuthClient.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(services => CreateHandler(services.GetRequiredService<WebhookConnection>()))
            .AddHttpMessageHandler<WebhookValidationHandler>();
        builder.Services.AddHttpClient(WebhookHttpClientFactory.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(services => CreateHandler(services.GetRequiredService<WebhookConnection>()))
            .AddHttpMessageHandler<WebhookValidationHandler>()
            .AddResilienceHandler($"{WebhookHttpClientFactory.HttpClientName}-pipeline", (pipeline, context) =>
            {
                var webhooks = context.ServiceProvider.GetRequiredService<IOptions<ChronicleOptions>>().Value.Webhooks;

                // Retry with exponential backoff
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(webhooks.RetryDelaySeconds),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = retryArguments => ValueTask.FromResult(HttpClientResiliencePredicates.IsTransient(retryArguments.Outcome))
                });

                // Circuit breaker
                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(webhooks.CircuitBreakerSamplingDurationSeconds),
                    FailureRatio = 0.5, // trip if 50% fail
                    MinimumThroughput = 10,
                    BreakDuration = TimeSpan.FromSeconds(webhooks.CircuitBreakerBreakDurationSeconds)
                });

                // Timeout per request
                pipeline.AddTimeout(TimeSpan.FromSeconds(webhooks.RequestTimeoutSeconds));
            });
        return builder;
    }

    static SocketsHttpHandler CreateHandler(WebhookConnection connection) => new()
    {
        AllowAutoRedirect = false,

        // A proxy would resolve/connect to the receiver on our behalf, bypassing address validation.
        UseProxy = false,
        ConnectCallback = connection.Connect
    };
}
