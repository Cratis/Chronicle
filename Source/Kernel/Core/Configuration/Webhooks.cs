// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for webhook observers.
/// </summary>
public class Webhooks
{
    /// <summary>
    /// The webhook retry delays in seconds used when <see cref="Observers.BackoffScheduleSeconds"/> is not configured.
    /// The last entry repeats.
    /// </summary>
    public static readonly IReadOnlyList<int> DefaultBackoffScheduleSeconds = [1, 5, 30, 120, 600, 1800, 3600];

    /// <summary>
    /// Gets the automatic quarantine probe policy, independent of delivery retry budgets.
    /// </summary>
    public WebhookProbe Probe { get; init; } = new();

    /// <summary>
    /// Gets the validation rollout mode. Enforce will become the default in the next major release.
    /// </summary>
    public WebhookValidationMode Validation { get; init; } = WebhookValidationMode.Warn;

    /// <summary>
    /// Gets whether HTTP is allowed in Development. Ignored outside Development.
    /// </summary>
    public bool AllowInsecureHttp { get; init; }

    /// <summary>
    /// Gets the global host allow-list, with exact hosts or wildcard subdomains such as *.example.com.
    /// </summary>
    public IEnumerable<string> AllowedHosts { get; init; } = [];

    /// <summary>
    /// Gets host allow-lists keyed by event-store name. An entry replaces, rather than extends, the global list.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> EventStoreAllowedHosts { get; init; } = new Dictionary<string, string[]>();

    /// <summary>
    /// Gets the operator-approved identities and scopes. Managed identity webhooks are disabled by default.
    /// </summary>
    public WebhookManagedIdentity ManagedIdentity { get; init; } = new();

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

    /// <summary>
    /// Gets the webhook-specific observer retry, parking and quarantine policy.
    /// These defaults are independent of the global observer retry settings.
    /// </summary>
    public Observers Retry { get; init; } = new()
    {
        MaxRetryAttempts = 0,
        MaximumBackoffDelay = 3600,
        RetryBudget = TimeSpan.FromHours(24),
        QuarantineOnFailedPartitionCount = 5,
        QuarantineOnFailedPartitionPercentage = 0.5
    };

    /// <summary>
    /// Gets the maximum accepted Retry-After delay in seconds. Longer receiver deadlines are clamped to this bound.
    /// </summary>
    public int MaximumRetryAfterSeconds { get; init; } = 24 * 60 * 60;
}
