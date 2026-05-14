// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for observers.
/// </summary>
public class Observers
{
    /// <summary>
    /// Gets the maximum number of observer partitions that can be handled concurrently.
    /// </summary>
    public int MaxConcurrentPartitions { get; init; } = 32;

    /// <summary>
    /// Gets the timeout in seconds for observer calling its subscriber.
    /// </summary>
    public int SubscriberTimeout { get; init; } = 5;

    /// <summary>
    /// Gets the maximum number of retries that can be attempted on a failed observer partition.
    /// </summary>
    /// <remarks>
    /// 0 represents infinite number of retries.
    /// </remarks>
    public int MaxRetryAttempts { get; init; } = 10;

    /// <summary>
    /// Gets the delay for attempting to retry a failed partition in seconds.
    /// </summary>
    public int BackoffDelay { get; init; } = 1;

    /// <summary>
    /// Gets the retry delay exponential factor.
    /// </summary>
    public float ExponentialBackoffDelayFactor { get; init; } = 2;

    /// <summary>
    /// Gets the max delay time in seconds for retrying a failed partition.
    /// </summary>
    public int MaximumBackoffDelay { get; init; } = 60 * 10;

    /// <summary>
    /// Gets whether observers should automatically replay when their definition changes.
    /// When enabled, projections, reducers, reactors, and webhooks replay immediately on definition change
    /// instead of creating a recommendation for manual replay.
    /// </summary>
    public bool ReplayOnDefinitionChange { get; init; }

    /// <summary>
    /// Gets the interval in seconds between watchdog checks on each observer.
    /// The watchdog verifies that connected clients are still active, that running jobs
    /// are progressing, and that the <c>NextEventSequenceNumber</c> is up-to-date.
    /// </summary>
    public int WatchdogInterval { get; init; } = 60;
}
