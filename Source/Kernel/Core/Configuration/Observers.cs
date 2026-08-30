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
    /// Gets how many seconds an observer waits for its subscriber to answer before giving up on the batch. Zero waits
    /// indefinitely.
    /// </summary>
    /// <remarks>
    /// The default matches the response timeout the transport already imposes, so it bounds nothing new on its own -
    /// the point of the knob is being able to bound it tighter for a subscriber that should always answer quickly.
    /// Raising it past the transport's own timeout has no effect, because that one gives up first. Giving up abandons
    /// the wait, not the work: the subscriber keeps processing the batch, and the events are redelivered when the
    /// partition retries. A timeout is recorded as <c>FailureKind.Timeout</c>, which is excluded from the quarantine
    /// thresholds below, so a congested period cannot take an otherwise healthy observer out of service.
    /// </remarks>
    public int SubscriberTimeout { get; init; } = 30;

    /// <summary>
    /// Gets the timeout in seconds to wait for an event store subscription to become ready before returning to the client.
    /// </summary>
    /// <remarks>
    /// Waiting ensures events are not lost if the client immediately starts publishing. When the timeout
    /// elapses, the subscription may still activate asynchronously and the client is not blocked further.
    /// </remarks>
    public int SubscriptionReadyTimeout { get; init; } = 5;

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
    /// Gets the threshold for quarantining an observer based on the number of failed partitions.
    /// A value of 0 disables observer quarantine based on failed partition count.
    /// </summary>
    public int QuarantineOnFailedPartitionCount { get; init; }

    /// <summary>
    /// Gets the threshold for quarantining an observer based on the percentage of failed partitions.
    /// A value of 0.0 disables observer quarantine based on failed partition percentage.
    /// </summary>
    public double QuarantineOnFailedPartitionPercentage { get; init; }

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

    /// <summary>
    /// Gets the number of consecutive progress-only batches after which the observer's
    /// <c>NextEventSequenceNumber</c> is made durable.
    /// </summary>
    /// <remarks>
    /// When an observer sees a batch that contains nothing it is subscribed to, it only advances
    /// <c>NextEventSequenceNumber</c> past the skipped events. Persisting that advance on every such
    /// batch is pure write amplification, so it is debounced: the state is written once this many
    /// progress-only batches have accumulated. The pending advance is also flushed on the watchdog
    /// tick (the time bound, governed by <see cref="WatchdogInterval"/>) and on deactivation. Catch-up
    /// recovers any progress not yet persisted after a crash — the re-scanned events are ones the
    /// observer already skipped and observers are idempotent, so nothing is lost or double-handled.
    /// A larger value trades a longer post-crash re-scan for fewer writes. Defaults to 100.
    /// </remarks>
    public int StatePersistenceBatchInterval { get; init; } = 100;

    /// <summary>
    /// Gets the strategy used to fan events out when multiple instances of the same client are
    /// connected. Supported values are "round-robin" (default - a deterministic distribution based
    /// on the partition key, keeping every partition sticky to one instance) and "random".
    /// </summary>
    public string FanOutStrategy { get; init; } = "round-robin";
}
