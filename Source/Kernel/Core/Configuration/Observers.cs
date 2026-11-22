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
    /// The recommended period, and the one used when <see cref="UnseenRemovalAfter"/> is not a positive value.
    /// </summary>
    public static readonly TimeSpan DefaultUnseenRemovalAfter = TimeSpan.FromDays(7);

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
    /// Gets the maximum number of gRPC reactor and reducer registrations running concurrently on a silo.
    /// </summary>
    /// <remarks>Defaults to 0 (unlimited). Values below 1 disable the gate. Does not limit event append or observer delivery.</remarks>
    public int MaxConcurrentRegistrations { get; init; }

    /// <summary>
    /// Gets the maximum number of projection subscriptions and failed-partition recoveries running concurrently on a silo.
    /// </summary>
    /// <remarks>
    /// One bound for the whole silo, shared by every event store on it, so a cold start does not run more of this work at
    /// once as the number of event stores grows. Defaults to 0 (automatic): two per processor, at least 4 and at most 16.
    /// Values below 1 select the automatic limit.
    /// </remarks>
    public int MaxConcurrentSubscriptions { get; init; }

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
    /// Gets the maximum number of events delivered inline in one live observer call. Zero or less disables the limit.
    /// </summary>
    /// <remarks>
    /// Larger batches are handed to a partition catch-up job without advancing the live position. Catch-up and replay
    /// are not limited by this budget.
    /// </remarks>
    public int LiveDeliveryBudget { get; init; } = 100;

    /// <summary>
    /// Gets the maximum time in seconds to hold an abandoned client delivery after its timeout.
    /// A non-positive value disables fencing. Positive values are at least the subscriber timeout.
    /// </summary>
    public int AbandonedDeliveryHold { get; init; } = 300;

    /// <summary>
    /// Gets the most events handed to a client-owned or webhook subscriber in one call during catch-up, replay and
    /// partition recovery. Zero or less hands over storage batches as they are read.
    /// </summary>
    /// <remarks>
    /// A storage batch can hold thousands of events, and a subscriber that does real work per event can need longer
    /// for that than any sensible <see cref="SubscriberTimeout"/>. Bounding the call bounds the work behind one answer,
    /// and every answered call is checkpointed, so a long partition advances call by call instead of needing an
    /// ever larger timeout. Kernel-owned subscribers, such as projections, are not bounded by this.
    /// </remarks>
    public int MaxEventsPerSubscriberCall { get; init; } = 100;

    /// <summary>
    /// Gets the most events in one page handed to a client reactor or reducer that declared page delivery, on every path.
    /// Zero or less leaves pages unbounded by count.
    /// </summary>
    /// <remarks>
    /// A client may declare a smaller bound and is capped by this one; a client that declares none gets this one. This only
    /// caps the pages a client declared; it never makes them larger than what a path hands over in the first place, so
    /// catch-up, replay and recovery stay bounded by <see cref="MaxEventsPerSubscriberCall"/> and live delivery by
    /// <see cref="LiveDeliveryBudget"/>. Defaults to 1 000.
    /// </remarks>
    public int MaxEventsPerPage { get; init; } = 1000;

    /// <summary>
    /// Gets the most bytes of event content, and for a reducer of the states it reduces from, handed to a client reactor
    /// or reducer that declared page delivery, in one call, on every path. Zero or less leaves pages unbounded by size.
    /// </summary>
    /// <remarks>
    /// An event's size is the UTF-8 length of its JSON content. A reducer page also holds the state of every partition in
    /// it, so the size the state had when it was last reduced counts toward the bound too; a state the kernel has not seen
    /// since it started counts once it has been read. A client may declare a smaller bound and is capped by this one; a
    /// client that declares none gets this one. An event larger than the bound is still delivered, alone in its page, so an
    /// oversize event never stalls its partition. <see cref="MaxEventsPerPage"/> caps the declared event count the same
    /// way. Defaults to 4 MiB, which is also the default most gRPC clients accept in one message; the page envelope -
    /// event metadata, partition keys and reducer states - comes on top of the event content, so a client that keeps its
    /// receive limit at that default should declare a smaller bound.
    /// </remarks>
    public int MaxBytesPerSubscriberCall { get; init; } = 4 * 1024 * 1024;

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
    /// Gets how many seconds an observer that has no subscription and no pending work stays resident before it is released.
    /// </summary>
    /// <remarks>
    /// Observers are kept resident for as long as they are subscribed, because their live subscription exists only in
    /// memory. An observer whose client is gone keeps nothing that is not in storage, so it is deactivated once the
    /// watchdog has seen it idle for this long, and the next call activates it again from storage. Checked on every
    /// <see cref="WatchdogInterval"/>. Defaults to 900 seconds (15 minutes); 0 keeps every observer resident.
    /// </remarks>
    public int IdleDeactivationAfter { get; init; } = 900;

    /// <summary>
    /// Gets whether, and how, an observer records its subscription in its state document.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="DurableSubscriptionMode.Release"/>: the subscription is written and cleared with the
    /// observer's state, and a subscribed observer that has nothing to do is released after <see cref="DormantAfter"/>
    /// and woken from the record. <see cref="DurableSubscriptionMode.Shadow"/> writes the record and compares it with
    /// the live subscriptions of the appended events queue without releasing or waking anything.
    /// <see cref="DurableSubscriptionMode.Off"/> writes none and clears any that is stored. A server-level setting
    /// (decision 0034).
    /// </remarks>
    public DurableSubscriptionMode DurableSubscriptions { get; init; } = DurableSubscriptionMode.Release;

    /// <summary>
    /// Gets how many seconds a subscribed observer that has nothing to do stays resident before it is released.
    /// </summary>
    /// <remarks>
    /// Defaults to 900 seconds (15 minutes), the same as <see cref="IdleDeactivationAfter"/>; 0 keeps every subscribed
    /// observer resident. A released observer is woken by the appended events queue when an event it observes arrives,
    /// and by the safety net (<see cref="DormantSafetyNet"/>). Applies to client reactors, client reducers and
    /// projections only, and only while <see cref="DurableSubscriptions"/> is <see cref="DurableSubscriptionMode.Release"/>
    /// and every silo of the cluster runs the same release, which supports it. An observer resumes from its persisted
    /// cursor when it wakes (decision 0034). A server-level setting.
    /// </remarks>
    public int DormantAfter { get; init; } = 900;

    /// <summary>
    /// Gets how many seconds pass at most before a dormant observer is woken, whether or not an event arrived.
    /// </summary>
    /// <remarks>
    /// Defaults to 900 seconds (15 minutes). Each observer's interval is jittered so wake-ups do not line up. It bounds
    /// how long a lost wake-up can delay delivery to a dormant observer. At each interval the queue compares the
    /// persisted cursor of the observer with the tail of the event types it observes and wakes it only when an event is
    /// waiting, or when the cursor cannot be compared; <see cref="DormantSafetyNetFullWake"/> sets how often it is woken
    /// regardless. The checks are driven by the appended events queue, so no timer or activation is held for a dormant
    /// observer. Values below 1 select the default. A server-level setting (decision 0034).
    /// </remarks>
    public int DormantSafetyNet { get; init; } = 900;

    /// <summary>
    /// Gets how many seconds pass at most before a dormant observer is woken whether or not an event is waiting for it.
    /// </summary>
    /// <remarks>
    /// The comparison made every <see cref="DormantSafetyNet"/> interval finds a lost wake-up, but it cannot see what
    /// only a running observer can: a client that left without notice, a definition that another registration took
    /// over, a record that no longer matches the observer. This unconditional wake finds those. Defaults to 3600
    /// seconds (60 minutes). Values below 1 select the default; a value below <see cref="DormantSafetyNet"/> is raised
    /// to it, which wakes every dormant observer at every interval as before the comparison existed. A server-level
    /// setting (decision 0034).
    /// </remarks>
    public int DormantSafetyNetFullWake { get; init; } = 3600;

    /// <summary>
    /// Gets how many dormant observers one silo wakes at the same time.
    /// </summary>
    /// <remarks>
    /// One event that many dormant observers watch for wakes all of them; this bounds how many are activated at once
    /// on a silo, so a burst becomes a short queue instead of a spike. The others wait for a place and are delivered
    /// to afterwards. Defaults to 0 (automatic): two per processor, at least 4 and at most 16, the same as
    /// <see cref="MaxConcurrentSubscriptions"/>. Values below 1 select the automatic limit. A server-level setting
    /// (decision 0034).
    /// </remarks>
    public int MaxConcurrentWakes { get; init; }

    /// <summary>
    /// Gets how many events a woken observer may read and deliver from its cursor before the wake falls back to a catch-up job.
    /// </summary>
    /// <remarks>
    /// Defaults to 1000. An observer that was dormant normally wakes for the one or two events that woke it; a larger gap
    /// is what the catch-up job exists for, so the observer resumes through it instead of delivering inside its
    /// activation. Values below 1 select the default. A server-level setting (decision 0034).
    /// </remarks>
    public int DormantResumeMaxEvents { get; init; } = 1000;

    /// <summary>
    /// Gets how often each appended events queue compares its live subscriptions with the stored durable ones.
    /// </summary>
    /// <remarks>
    /// Defaults to 15 minutes. The comparison reads the observer states of the queue's event store and namespace once
    /// per interval and only counts disagreements; it never changes a subscription. It runs only while
    /// <see cref="DurableSubscriptions"/> is <see cref="DurableSubscriptionMode.Shadow"/>. A zero or negative value
    /// falls back to the default.
    /// </remarks>
    public TimeSpan DurableSubscriptionCheckInterval { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets whether the kernel removes client reactors and reducers that no client has subscribed to for <see cref="UnseenRemovalAfter"/>.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false"/>: nothing is ever removed unless an operator turns this on, because a removed reactor replays
    /// from the start when its client returns and handles the whole history again. While it is off the kernel does not sweep
    /// and removes nothing, but it still records when each client observer was last seen, so turning the removal on later does
    /// not start every clock from zero. This is a server-level setting.
    /// </remarks>
    public bool UnseenRemovalEnabled { get; init; }

    /// <summary>
    /// Gets how long a client reactor or reducer can go without any client holding a subscription to it, in every namespace,
    /// before the kernel removes it and its records, when <see cref="UnseenRemovalEnabled"/> is on.
    /// </summary>
    /// <remarks>
    /// Defaults to 7 days, which is the recommended period; a zero or negative value falls back to the default. Removal
    /// deletes the observer's definition, state, failed partitions, jobs and reminders, never a reducer's read model data.
    /// When the client connects again the observer is registered as new and replays from the start, so a reactor handles the
    /// whole history again. Projections, webhooks and kernel observers are never removed. This is a server-level setting.
    /// </remarks>
    public TimeSpan UnseenRemovalAfter { get; init; } = DefaultUnseenRemovalAfter;

    /// <summary>
    /// Gets how often each silo looks for client observers that have not been seen for <see cref="UnseenRemovalAfter"/>, when <see cref="UnseenRemovalEnabled"/> is on.
    /// </summary>
    /// <remarks>
    /// Defaults to 1 hour. The sweep reads storage and only activates an observer that is a candidate for removal. A zero or
    /// negative value falls back to the default.
    /// </remarks>
    public TimeSpan UnseenRemovalSweepInterval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets how many observers one storage read of the unseen-observer sweep covers.
    /// </summary>
    /// <remarks>
    /// Defaults to 100. A value below 1 falls back to the default.
    /// </remarks>
    public int UnseenRemovalBatchSize { get; init; } = 100;

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
