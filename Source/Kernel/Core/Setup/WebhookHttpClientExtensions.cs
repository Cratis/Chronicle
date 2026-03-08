// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
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
        builder.Services.AddHttpClient(WebhookHttpClientFactory.HttpClientName)
            .AddResilienceHandler($"{WebhookHttpClientFactory.HttpClientName}-pipeline", pipeline =>
            {
                // Retry with exponential backoff
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = retryArguments => ValueTask.FromResult(HttpClientResiliencePredicates.IsTransient(retryArguments.Outcome))
                });

                // Circuit breaker
                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    FailureRatio = 0.5, // trip if 50% fail
                    MinimumThroughput = 10,
                    BreakDuration = TimeSpan.FromSeconds(15)
                });

                // Timeout per request
                pipeline.AddTimeout(TimeSpan.FromSeconds(60));
            });
        return builder;
    }
}
