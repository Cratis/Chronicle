// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for observers.
/// </summary>
public class Observers
{
    /// <summary>
    /// Gets the upper bound on how many job steps run in parallel.
    /// </summary>
    /// <remarks>
    /// Despite the name, this does not limit live event delivery: live delivery never processes partitions of one
    /// observer concurrently. It only caps parallel job steps (replay and catch-up work), together with
    /// <see cref="Jobs.MaxParallelSteps"/>: the effective limit is the smaller of
    /// <see cref="Jobs.GetEffectiveMaxParallelSteps"/> and this value.
    /// </remarks>
    public int MaxConcurrentPartitions { get; init; } = 32;

    /// <summary>
    /// Gets how many seconds an observer waits for its subscriber to answer before giving up on the batch. Zero waits
    /// indefinitely.
    /// </summary>
    /// <remarks>
    /// The call to a subscriber carries its own Orleans response timeout (see
    /// <see cref="Observation.ObserverSubscriberResponseTimeout"/>), so this setting is not capped by
    /// <c language="csharp">clustering.responseTimeout</c>: raising it lets a subscriber that needs longer per batch take longer.
    /// Zero, and anything above the ceiling, is bounded by that ceiling, and the kernel warns about it at startup. Giving up abandons
    /// the wait, not the work: the subscriber keeps processing the batch, and the events are redelivered when the
    /// partition retries. A timeout is recorded as <c language="csharp">FailureKind.Timeout</c>, which is excluded from the quarantine
    /// thresholds below, so a congested period cannot take an otherwise healthy observer out of service.
    /// </remarks>
    public int SubscriberTimeout { get; init; } = 30;

    /// <summary>
    /// Gets the maximum time in seconds to hold an abandoned client delivery after its timeout.
    /// A non-positive value disables fencing. Positive values are at least the subscriber timeout.
    /// </summary>
    public int AbandonedDeliveryHold { get; init; } = 300;

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
    /// Gets the webhook retry delays in seconds. After the last entry, that delay repeats.
    /// When empty, webhooks use <see cref="Webhooks.DefaultBackoffScheduleSeconds"/>. Only used by Webhooks:Retry.
    /// </summary>
    /// <remarks>
    /// The default stays empty because configuration binding appends configured entries to an existing collection.
    /// </remarks>
    public IReadOnlyList<int> BackoffScheduleSeconds { get; init; } = [];

    /// <summary>
    /// Gets the webhook retry budget from the first attempt since quarantine was cleared.
    /// Zero disables the elapsed-time limit. Only used by Webhooks:Retry.
    /// </summary>
    public TimeSpan RetryBudget { get; init; }

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
    /// Gets the policy for evolving read models when a projection or reducer definition changes.
    /// </summary>
    public DefinitionEvolutionPolicy DefinitionEvolution { get; init; } = DefinitionEvolutionPolicy.Automatic;

    /// <summary>
    /// Gets the promotion gate for projection generations. Takes effect only when <see cref="Features.ProjectionGenerations"/> is enabled.
    /// </summary>
    public PromotionGateMode GenerationPromotion { get; init; } = PromotionGateMode.OnCatchUp;

    /// <summary>
    /// Gets how long the deployment condition must hold before promotion. Takes effect only when <see cref="Features.ProjectionGenerations"/> is enabled.
    /// </summary>
    public TimeSpan DeploymentConditionGracePeriod { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets how long a generation may remain pending before an alert is raised. Takes effect only when <see cref="Features.ProjectionGenerations"/> is enabled.
    /// </summary>
    public TimeSpan PendingGenerationAlertAfter { get; init; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets whether reactors and webhooks should automatically replay when their definition changes.
    /// </summary>
    /// <remarks>
    /// Projection and reducer changes use <see cref="DefinitionEvolution"/> because Chronicle can classify those
    /// changes and avoid a full replay when less work is safe.
    /// </remarks>
    public bool ReplayOnDefinitionChange { get; init; }

    /// <summary>
    /// Gets the interval in seconds between watchdog checks on each observer.
    /// The watchdog verifies that connected clients are still active, that running jobs
    /// are progressing, and that the <c language="csharp">NextEventSequenceNumber</c> is up-to-date.
    /// </summary>
    public int WatchdogInterval { get; init; } = 60;

    /// <summary>
    /// Gets the number of consecutive progress-only batches after which the observer's
    /// <c language="csharp">NextEventSequenceNumber</c> is made durable.
    /// </summary>
    /// <remarks>
    /// When an observer sees a batch that contains nothing it is subscribed to, it only advances
    /// <c language="csharp">NextEventSequenceNumber</c> past the skipped events. Persisting that advance on every such
    /// batch is pure write amplification, so it is debounced: the state is written once this many
    /// progress-only batches have accumulated. The pending advance is also flushed on the watchdog
    /// tick (the time bound, governed by <see cref="WatchdogInterval"/>) and on deactivation. Catch-up
    /// recovers any progress not yet persisted after a crash — the re-scanned events are ones the
    /// observer already skipped and observers are idempotent, so nothing is lost or double-handled.
    /// A larger value trades a longer post-crash re-scan for fewer writes. Defaults to 100.
    /// </remarks>
    public int StatePersistenceBatchInterval { get; init; } = 100;

    /// <summary>
    /// Gets the maximum time in seconds the observer holds back reported handled-event counts and the follow-up writes of partition failures before making them durable.
    /// </summary>
    /// <remarks>
    /// Every handled batch of every job step reports its counts to the single observer grain. Writing them through
    /// - an increment per partition plus the whole observer state - on each report makes the grain's queue grow
    /// past the grain call timeout under a replay. Reports are therefore coalesced per partition and event type and
    /// written together after at most this many seconds, on the watchdog tick and on deactivation. The sums are
    /// exact; only their freshness is bounded. A partition failure is recorded in the observer's memory and in its
    /// failed partitions storage straight away, one partition at a time; the observer state write and the retry
    /// reminder registration, which are the same for every failure of a burst, wait for the same flush instead of
    /// holding up the observer once per failure. 0 writes everything through. Defaults to 5.
    /// </remarks>
    public int WriteCoalescingInterval { get; init; } = 5;

    /// <summary>
    /// Gets the strategy used to fan events out when multiple instances of the same client are
    /// connected. Supported values are "round-robin" (default - a deterministic distribution based
    /// on the partition key, keeping every partition sticky to one instance) and "random".
    /// </summary>
    public string FanOutStrategy { get; init; } = "round-robin";

    /// <summary>
    /// Gets the maximum number of consecutive times the watchdog will recover an observer stuck "preparing
    /// catch-up" with no catch-up job driving it forward before giving up and quarantining the observer instead.
    /// </summary>
    /// <remarks>
    /// Starting a catch-up job can fail for reasons a retry cannot fix - a persistently unreachable job subsystem,
    /// for example - which turns unconditional retrying into a busy loop: every watchdog tick clears the stranded
    /// flag, re-routes the observer, asks for a new catch-up job, fails again, and is found stranded again next
    /// tick, forever, with zero forward progress. Bounding the count and quarantining once it is exceeded turns
    /// that silent, permanent spin into a visible, operator-actionable state. Unlike <see cref="MaxRetryAttempts"/>,
    /// 0 is not treated as infinite here - a value of 0 or less always quarantines on the very first stranded
    /// recovery, since leaving this unbounded reintroduces the loop this setting exists to close.
    /// <para>
    /// The same bound applies to the watchdog retrying the recovery a subscribed observer is owed once its quarantine
    /// is cleared, when that recovery failed and left the observer disconnected.
    /// </para>
    /// </remarks>
    public int MaxCatchupRecoveryAttempts { get; init; } = 5;
}
