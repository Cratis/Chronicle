// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for webhook observers.
/// </summary>
public class Webhooks
{
    /// <summary>
    /// Gets the delay in seconds used as the base for the exponential backoff between webhook delivery retries.
    /// </summary>
    public int RetryDelaySeconds { get; init; } = 2;

    /// <summary>
    /// Gets the duration in seconds over which failures are sampled by the webhook delivery circuit breaker.
    /// </summary>
    public int CircuitBreakerSamplingDurationSeconds { get; init; } = 30;

    /// <summary>
    /// Gets the duration in seconds the webhook delivery circuit breaker stays open after it trips.
    /// </summary>
    public int CircuitBreakerBreakDurationSeconds { get; init; } = 15;

    /// <summary>
    /// Gets the timeout in seconds applied to each individual webhook delivery request.
    /// </summary>
    public int RequestTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Gets the timeout in seconds applied when testing a webhook endpoint.
    /// </summary>
    public int TestTimeoutSeconds { get; init; } = 10;
}
