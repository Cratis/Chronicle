// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Orleans.CodeGeneration;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines an observer in the system.
/// </summary>
/// <remarks>
/// The observer is implemented as a state machine internally, but that is an implementation concern and
/// deliberately not part of this grain contract - state transitions are driven from within the observer
/// implementation, never across a grain reference.
/// </remarks>
[Version(3)]
public interface IObserver : IGrainWithStringKey
{
    /// <summary>
    /// Ensure the observer existence.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Interleaved because it does nothing - its whole purpose is to force activation, so there is no
    /// state for non-reentrancy to protect. Without this it queued behind whatever the observer was
    /// already doing, and the startup task fans one of these out across every reducer and reactor at
    /// once: a single slow Subscribe put the whole fan-out past Orleans' response timeout and took the
    /// silo down with it.
    /// </remarks>
    [AlwaysInterleave]
    Task Ensure();

    /// <summary>
    /// Get the definition from the observer.
    /// </summary>
    /// <returns>The <see cref="ObserverDefinition"/>.</returns>
    [AlwaysInterleave]
    Task<ObserverDefinition> GetDefinition();

    /// <summary>
    /// Get the state from the observer.
    /// </summary>
    /// <returns>The <see cref="ObserverState"/>.</returns>
    [AlwaysInterleave]
    Task<ObserverState> GetState();

    /// <summary>
    /// Set the handled stats for the observer.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The last handled event sequence number.</param>
    /// <returns>Awaitable task.</returns>
    Task SetHandledStats(EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Report a batch of successfully handled events for a partition as counts broken down by event type.
    /// Increments both the aggregate and per-event-type handled event counts, as well as
    /// the per-partition breakdown used to subtract counts when a partition is replayed.
    /// </summary>
    /// <param name="partition">The <see cref="Key"/> of the partition that handled the events.</param>
    /// <param name="countsPerEventType">The number of successfully handled events, broken down by <see cref="EventTypeId"/>.</param>
    /// <param name="reportedBy">The <see cref="JobStepId"/> of the job step reporting the counts.</param>
    /// <param name="handledUpTo">The <see cref="EventSequenceNumber"/> of the last event the counts cover.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// <para>
    /// Only the counts are needed to maintain the observer's handled-event statistics, so callers pass those
    /// rather than the full <see cref="AppendedEvent"/> payloads. On the replay path this avoids shipping the
    /// decrypted events across the grain boundary a second time when the observer only reads their type ids.
    /// </para>
    /// <para>
    /// With write coalescing on, the call only merges the counts in memory, so a catch-up over many partitions does not
    /// queue one storage write per step behind the observer's other turns past the response timeout. It does not
    /// interleave: with write coalescing turned off it writes through, and that write must not overlap another turn's
    /// state changes.
    /// A report is identified by <paramref name="reportedBy"/>, <paramref name="partition"/> and <paramref name="handledUpTo"/>, so a step can send it
    /// again after a timeout without the counts being taken twice.
    /// </para>
    /// </remarks>
    Task ReportHandledEvents(Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType, JobStepId reportedBy, EventSequenceNumber handledUpTo);

    /// <summary>
    /// Reports one page of replay handled counts, retaining each partition's deduplication identity.
    /// </summary>
    /// <param name="reportedBy">The reporting job step.</param>
    /// <param name="reports">The counts and last handled sequence number for each partition.</param>
    /// <returns>Awaitable task.</returns>
    Task ReportHandledEventsBatch(JobStepId reportedBy, IReadOnlyList<Jobs.HandledEventsReport> reports);

    /// <summary>
    /// Get the subscription for the observer.
    /// </summary>
    /// <returns>Tbe <see cref="ObserverSubscription"/>.</returns>
    [AlwaysInterleave]
    Task<ObserverSubscription> GetSubscription();

    /// <summary>
    /// Gets active abandoned-delivery fences independently of failure state.
    /// </summary>
    /// <returns>The current fences.</returns>
    [AlwaysInterleave]
    Task<IEnumerable<DeliveryFence>> GetDeliveryFences();

    /// <summary>
    /// Releases the exact version whose original targets have drained.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="releaseBy">The captured fence deadline.</param>
    /// <returns>A task representing the release.</returns>
    [AlwaysInterleave]
    Task ReleaseDeliveryFence(Key partition, DateTimeOffset releaseBy);

    /// <summary>
    /// Checks whether a subscription is installed.
    /// </summary>
    /// <returns>True if a subscription is installed, including while its setup is in progress.</returns>
    [AlwaysInterleave]
    Task<bool> IsSubscribed();

    /// <summary>
    /// Checks whether a subscription is installed and its setup completed successfully.
    /// </summary>
    /// <returns>True if setup completed, including while healthy catch-up is pending; false if missing, in progress or failed.</returns>
    [AlwaysInterleave]
    Task<bool> IsSubscriptionEstablished();

    /// <summary>
    /// Get the event types that the observer is observing.
    /// </summary>
    /// <returns>Collection of <see cref="EventType"/>.</returns>
    [AlwaysInterleave]
    Task<IEnumerable<EventType>> GetEventTypes();

    /// <summary>
    /// Subscribe to observer.
    /// </summary>
    /// <typeparam name="TObserverSubscriber">Type of <see cref="IObserverSubscriber"/> to subscribe.</typeparam>
    /// <param name="type"><see cref="ObserverType"/>.</param>
    /// <param name="eventTypes">Collection of <see cref="EventType">event types</see> to subscribe to.</param>
    /// <param name="siloAddress"><see cref="SiloAddress"/> the subscriber is connected to.</param>
    /// <param name="subscriberArgs">Optional arguments associated with the subscription.</param>
    /// <param name="isReplayable">Whether the observer supports replay scenarios. Defaults to true.</param>
    /// <param name="filters">Optional <see cref="ObserverFilters"/> to apply when observing events.</param>
    /// <param name="reactivateRetired">Whether explicit registration may start a fresh lifecycle for a retired observer. Automatic recovery must pass false.</param>
    /// <param name="automatic">Whether this subscription is automatic reconciliation, which updates the subscription without ending quarantine.</param>
    /// <returns>Awaitable task.</returns>
    Task Subscribe<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs = default,
        bool isReplayable = true,
        ObserverFilters? filters = default,
        bool reactivateRetired = true,
        bool automatic = false)
        where TObserverSubscriber : IObserverSubscriber;

    /// <summary>
    /// Subscribes with an explicit generation delivery policy without changing the legacy grain method.
    /// </summary>
    /// <typeparam name="TObserverSubscriber">The subscriber type.</typeparam>
    /// <param name="type">The observer type.</param>
    /// <param name="eventTypes">The subscribed event types.</param>
    /// <param name="siloAddress">The subscriber's silo.</param>
    /// <param name="generationDelivery">The generation delivery policy.</param>
    /// <param name="subscriberArgs">The subscriber arguments.</param>
    /// <param name="isReplayable">Whether the observer supports replay.</param>
    /// <param name="filters">The observation filters.</param>
    /// <param name="reactivateRetired">Whether retired observers may reactivate.</param>
    /// <param name="automatic">Whether registration is automatic.</param>
    /// <returns>Awaitable task.</returns>
    Task SubscribeWithGenerationDelivery<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        EventGenerationDelivery generationDelivery,
        object? subscriberArgs = default,
        bool isReplayable = true,
        ObserverFilters? filters = default,
        bool reactivateRetired = true,
        bool automatic = false)
        where TObserverSubscriber : IObserverSubscriber =>
        generationDelivery == EventGenerationDelivery.Compatibility
            ? Subscribe<TObserverSubscriber>(type, eventTypes, siloAddress, subscriberArgs, isReplayable, filters, reactivateRetired, automatic)
            : Task.FromException(new EventGenerationDeliveryNotSupported());

    /// <summary>
    /// Subscribe a kernel observer additively, retaining the highest generation of every already subscribed type.
    /// </summary>
    /// <typeparam name="TObserverSubscriber">The kernel-owned subscriber type.</typeparam>
    /// <param name="type">The observer type.</param>
    /// <param name="eventTypes">The event types to observe.</param>
    /// <param name="siloAddress">The subscriber's silo address.</param>
    /// <param name="subscriberArgs">Optional subscriber arguments.</param>
    /// <param name="isReplayable">Whether the observer supports replay.</param>
    /// <param name="filters">Optional event filters.</param>
    /// <returns>The effective merged event types from this non-interleaved grain request. Retired or sealed observers are left unchanged.</returns>
    Task<IEnumerable<EventType>> SubscribeAdditively<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs = default,
        bool isReplayable = true,
        ObserverFilters? filters = default)
        where TObserverSubscriber : IObserverSubscriber;

    /// <summary>
    /// Check whether a kernel subscription is missing, has failed setup, or observes a different event-type set.
    /// </summary>
    /// <remarks>
    /// This interleaved, read-only hint avoids unnecessary definition writes. Recovery must recheck in its own
    /// serialized request because setup or quarantine can change before it runs. Retired or sealed observers never need recovery.
    /// </remarks>
    /// <param name="eventTypes">The expected event types.</param>
    /// <returns>Whether subscription recovery may be needed.</returns>
    [AlwaysInterleave]
    Task<bool> NeedsSubscriptionRecovery(IEnumerable<EventType> eventTypes);

    /// <summary>
    /// Recover a missing, failed, or outdated explicitly typed kernel subscription without releasing quarantine or resetting progress.
    /// </summary>
    /// <remarks>
    /// The decision and recovery run in one non-interleaved grain request. A subscribed observer is retried after
    /// setup has failed and left it stalled, or after healthy setup if its event types have changed. Recovery
    /// waits for any running setup request before making this decision. Retired or sealed observers are left unchanged.
    /// This kernel-only operation is not exposed through the client contracts.
    /// </remarks>
    /// <typeparam name="TObserverSubscriber">The kernel-owned subscriber type.</typeparam>
    /// <param name="type">The observer type.</param>
    /// <param name="eventTypes">The event types to observe.</param>
    /// <param name="siloAddress">The subscriber's silo address.</param>
    /// <param name="subscriberArgs">Optional subscriber arguments.</param>
    /// <param name="isReplayable">Whether the observer supports replay.</param>
    /// <param name="filters">Optional event filters.</param>
    /// <returns>The effective event types, including registrations merged in this grain request.</returns>
    Task<IEnumerable<EventType>> RecoverStalledSubscription<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        object? subscriberArgs = default,
        bool isReplayable = true,
        ObserverFilters? filters = default)
        where TObserverSubscriber : IObserverSubscriber;

    /// <summary>
    /// Checks whether this activation knows a client, as a subscription target or as a superseded registration.
    /// </summary>
    /// <remarks>
    /// An interleaved, read-only hint for the grain holding a client registration. A new activation - after the
    /// silo that hosted the previous one was lost - starts out knowing no client.
    /// </remarks>
    /// <param name="connectionId">The <see cref="ConnectionId"/> of the client.</param>
    /// <returns>True if the client is known; otherwise false.</returns>
    [AlwaysInterleave]
    Task<bool> KnowsClient(ConnectionId connectionId);

    /// <summary>
    /// Restore the subscription of a connected client that this activation does not know about.
    /// </summary>
    /// <remarks>
    /// The subscription is activation-only state. When the silo hosting an observer is lost, the observer comes back
    /// on another silo with no subscription while the client stays connected and its registration grain survives.
    /// The registration grain calls this to put the client back. It does nothing if the client is already known,
    /// the observer is sealed, retired or quarantined, operator-cleared recovery is still owed, or the namespace no
    /// longer exists. Like <see cref="ReconcileSubscription"/>, it never ends quarantine or resets live progress.
    /// </remarks>
    /// <typeparam name="TObserverSubscriber">The client-owned subscriber type.</typeparam>
    /// <param name="type">The observer type.</param>
    /// <param name="eventTypes">The event types the client registered.</param>
    /// <param name="siloAddress">The silo address of the client's connection.</param>
    /// <param name="connectedClient">The client to restore.</param>
    /// <param name="isReplayable">Whether the observer supports replay.</param>
    /// <param name="filters">Optional event filters.</param>
    /// <param name="generationDelivery">The generation delivery policy.</param>
    /// <returns>Awaitable task.</returns>
    Task RestoreClientSubscription<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        ConnectedClient connectedClient,
        bool isReplayable = true,
        ObserverFilters? filters = default,
        EventGenerationDelivery generationDelivery = EventGenerationDelivery.Compatibility)
        where TObserverSubscriber : IObserverSubscriber;

    /// <summary>
    /// Reconcile a kernel subscription in a serialized request without ending quarantine or resetting live progress.
    /// </summary>
    /// <remarks>
    /// Replaces membership with the exact supplied pins and delivery policy, or preserves wildcard future-type
    /// coverage. Sealed, retired, quarantined observers and recovery still owed after operator clearance are left
    /// unchanged. Eligibility is rechecked after awaited setup before installation and recovery.
    /// This kernel-only operation is not exposed through client contracts.
    /// </remarks>
    /// <typeparam name="TObserverSubscriber">The kernel-owned subscriber type.</typeparam>
    /// <param name="type">The observer type.</param>
    /// <param name="eventTypes">The exact replacement event types and generation pins.</param>
    /// <param name="siloAddress">The subscriber's silo address.</param>
    /// <param name="subscribesToAllEvents">Whether to observe all current and future types instead of a fixed set.</param>
    /// <param name="generationDelivery">The generation delivery policy.</param>
    /// <param name="subscriberArgs">Optional subscriber arguments.</param>
    /// <param name="isReplayable">Whether the observer supports replay.</param>
    /// <param name="filters">Optional event filters.</param>
    /// <returns>Awaitable task.</returns>
    Task ReconcileSubscription<TObserverSubscriber>(
        ObserverType type,
        IEnumerable<EventType> eventTypes,
        SiloAddress siloAddress,
        bool subscribesToAllEvents,
        EventGenerationDelivery generationDelivery = EventGenerationDelivery.Compatibility,
        object? subscriberArgs = default,
        bool isReplayable = true,
        ObserverFilters? filters = default)
        where TObserverSubscriber : IObserverSubscriber;

    /// <summary>
    /// Subscribe to all event types in the observer, respecting retirement during automatic recovery.
    /// </summary>
    /// <typeparam name="TObserverSubscriber">Type of <see cref="IObserverSubscriber"/> to subscribe.</typeparam>
    /// <param name="type"><see cref="ObserverType"/>.</param>
    /// <param name="siloAddress"><see cref="SiloAddress"/> the subscriber is connected to.</param>
    /// <param name="subscriberArgs">Optional arguments associated with the subscription.</param>
    /// <param name="isReplayable">Whether the observer supports replay scenarios. Defaults to true.</param>
    /// <param name="reactivateRetired">Whether explicit registration may start a fresh lifecycle for a retired observer. Automatic recovery must pass false.</param>
    /// <returns>Awaitable task. A retired observer is left unsubscribed during automatic recovery.</returns>
    Task SubscribeToAllEvents<TObserverSubscriber>(
        ObserverType type,
        SiloAddress siloAddress,
        object? subscriberArgs = default,
        bool isReplayable = true,
        bool reactivateRetired = true)
        where TObserverSubscriber : IObserverSubscriber;

    /// <summary>
    /// Unsubscribe from the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe();

    /// <summary>
    /// Unsubscribe from the observer only if the current subscription was made by the given client.
    /// </summary>
    /// <remarks>
    /// This is an atomic check-and-unsubscribe operation. When a client reconnects, the old client's
    /// stream cleanup races against the new client's <c language="csharp">Subscribe</c> call: if the new client has
    /// already replaced the subscription, the old client's <c language="csharp">Unsubscribe</c> must be a no-op.
    /// Doing the check inside the grain prevents the TOCTOU race where the subscription is read
    /// (still owned by the old client), then replaced by the new client, then unsubscribed by the
    /// old client — destroying the new subscription.
    /// </remarks>
    /// <param name="connectionId">The <see cref="ConnectionId"/> of the client that owns the
    /// subscription it expects to unsubscribe.</param>
    /// <returns>Awaitable task.</returns>
    Task UnsubscribeIfMatchesClient(ConnectionId connectionId);

    /// <summary>
    /// Rewind the observer.
    /// </summary>
    /// <returns>The <see cref="JobId"/> of the replay job that was started or resumed, or <see cref="JobId.NotSet"/> if the observer is not replayable or not in a state it can replay from (disconnected, quarantined).</returns>
    Task<JobId> Replay();

    /// <summary>
    /// Rewind the observer for a specific partition.
    /// </summary>
    /// <param name="partition">The partition to rewind.</param>
    /// <returns>Awaitable task.</returns>
    Task ReplayPartition(Key partition);

    /// <summary>
    /// Replays selected event types for a specific partition without rebuilding unaffected contributions.
    /// </summary>
    /// <param name="partition">The partition to replay.</param>
    /// <param name="eventTypes">The affected event types to apply.</param>
    /// <returns>Awaitable task.</returns>
    Task ReplayPartition(Key partition, IEnumerable<EventType> eventTypes);

    /// <summary>
    /// Rewind the observer for a specific partition to a specific sequence number.
    /// </summary>
    /// <param name="partition">The partition to rewind.</param>
    /// <param name="sequenceNumber"><see cref="EventSequenceNumber"/> to rewind to.</param>
    /// <returns>Awaitable task.</returns>
    Task ReplayPartitionTo(Key partition, EventSequenceNumber sequenceNumber);

    /// <summary>
    /// Notify that the observer has been replayed.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The <see cref="EventSequenceNumber"/> it has been replayed to.</param>
    /// <returns>Awaitable task.</returns>
    Task Replayed(EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Releases a refused full replay without changing progress or resolving failed partitions.
    /// </summary>
    /// <remarks>Leaves retired, removed and non-replaying observers unchanged; quarantined observers remain quarantined.</remarks>
    /// <returns>Awaitable task.</returns>
    Task ReleaseRefusedReplay();

    /// <summary>
    /// Releases a refused partition replay and probes catch-up from its persisted live-delivery lower bound.
    /// </summary>
    /// <param name="partition">The partition whose replay was refused.</param>
    /// <param name="heldBackFrom">The first held-back position, or unavailable for a legacy job requiring operator recovery.</param>
    /// <remarks>Does not advance progress or resolve failed partitions. Unknown bounds quarantine the partition; retired, removed and non-replaying partitions are unchanged.</remarks>
    /// <returns>Awaitable task.</returns>
    Task ReleaseRefusedPartitionReplay(Key partition, EventSequenceNumber heldBackFrom);

    /// <summary>
    /// Complete a replay, resolving only failures already present when it began.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The last event handled by the replay.</param>
    /// <param name="replayedPartitions">Partitions and their successfully handled sequence numbers.</param>
    /// <param name="replayedEventTypes">The event types read during the replay.</param>
    /// <param name="replayStartedAt">The time before replay steps were prepared.</param>
    /// <returns>Awaitable task.</returns>
    Task ReplayedSuccessfullySince(EventSequenceNumber lastHandledEventSequenceNumber, IReadOnlyDictionary<Key, EventSequenceNumber> replayedPartitions, EventType[] replayedEventTypes, DateTimeOffset replayStartedAt);

    /// <summary>
    /// Notify that the partition has been replayed.
    /// </summary>
    /// <param name="partition">The partition that has been replayed.</param>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that was handled in the catchup.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Notify that a partition replay completed for the specified event types.
    /// </summary>
    /// <param name="partition">The partition that has been replayed.</param>
    /// <param name="lastHandledEventSequenceNumber">The last event handled by that partition.</param>
    /// <param name="replayedEventTypes">The event types included in the replay.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventType[] replayedEventTypes);

    /// <summary>
    /// Notify that a partition replay finished without handling all events.
    /// </summary>
    /// <param name="partition">The partition being replayed.</param>
    /// <param name="lastHandledEventSequenceNumber">The last event sequence number handled before replay stopped.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionReplayPartiallyCompleted(Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Notify that a partition replay finished, including how far the replay read.
    /// </summary>
    /// <param name="partition">The partition being replayed.</param>
    /// <param name="lastHandledEventSequenceNumber">The last event sequence number handled by the replay, or <see cref="EventSequenceNumber.Unavailable"/> when it handled nothing.</param>
    /// <param name="lastScannedEventSequenceNumber">The event sequence number of the last event the replay read for the partition, handled or excluded by the observer's filters, or <see cref="EventSequenceNumber.Unavailable"/> when the replay did not complete.</param>
    /// <param name="replayedEventTypes">The event types included in the replay, or empty when the replay cannot prove it handled the partition's failed event. Non-empty only when the replay completed and covered every event type; a replay that read nothing then hands the partition to catch-up from its start.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber, EventType[] replayedEventTypes);

    /// <summary>
    /// Notify that the partition has failed.
    /// </summary>
    /// <param name="partition">The partition that failed.</param>
    /// <param name="sequenceNumber">The sequence number of the failure.</param>
    /// <param name="exceptionMessages">All exception messages.</param>
    /// <param name="exceptionStackTrace">The exception stacktrace.</param>
    /// <param name="kind">What kind of thing went wrong. Defaults to <see cref="FailureKind.Unknown"/>.</param>
    /// <param name="episodeId">The recovery episode, or null for live delivery.</param>
    /// <param name="deferralId">The deferral that owned the recovery.</param>
    /// <param name="failurePolicy">The internal webhook recovery decision. Other subscribers use the default retry policy.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionFailed(Key partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace, FailureKind kind = FailureKind.Unknown, FailedPartitionId? episodeId = null, Guid? deferralId = null, WebhookDeliveryFailurePolicy failurePolicy = WebhookDeliveryFailurePolicy.Retry);

    /// <summary>
    /// Notify that the partition has failed with the original delivery fence.
    /// </summary>
    /// <param name="partition">The partition that failed.</param>
    /// <param name="sequenceNumber">The sequence number of the failure.</param>
    /// <param name="exceptionMessages">All exception messages.</param>
    /// <param name="exceptionStackTrace">The exception stacktrace.</param>
    /// <param name="kind">What kind of thing went wrong.</param>
    /// <param name="context">The internal recovery policy and optional original delivery identity and deadline.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionFailedWithDeliveryFence(Key partition, EventSequenceNumber sequenceNumber, IEnumerable<string> exceptionMessages, string exceptionStackTrace, FailureKind kind, PartitionFailureContext context);

    /// <summary>
    /// Persist a delivery deadline without consuming an attempt.
    /// </summary>
    /// <param name="partition">The partition to defer.</param>
    /// <param name="deferral">The deadline and first unhandled event.</param>
    /// <param name="episodeId">The recovery episode, or null for live delivery.</param>
    /// <param name="expectedDeferralId">The deferral that owned the delivery.</param>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task PartitionDeferred(Key partition, PartitionDeferral deferral, FailedPartitionId? episodeId = null, Guid? expectedDeferralId = null);

    /// <summary>
    /// Check recovery ownership and the persisted deadline before delivering.
    /// </summary>
    /// <param name="partition">The partition to deliver.</param>
    /// <param name="episodeId">The recovery episode, or null for a legacy job.</param>
    /// <param name="deferralId">The deferral that owns the delivery.</param>
    /// <returns>Whether delivery may resume now.</returns>
    [AlwaysInterleave]
    Task<bool> CanResumeFailedPartition(Key partition, FailedPartitionId? episodeId = null, Guid? deferralId = null);

    /// <summary>
    /// Records a projection bulk flush's failed partitions with one alert snapshot for the batch.
    /// </summary>
    /// <param name="failedPartitions">The failures returned by the projection sink.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionsFailed(IReadOnlyCollection<Storage.Sinks.FailedPartition> failedPartitions);

    /// <summary>
    /// Notify that the partition has recovered.
    /// </summary>
    /// <param name="partition">The partition that has recovered.</param>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that was handled in the catchup.</param>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task FailedPartitionRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Notify that the partition has recovered, including how far the recovery read.
    /// </summary>
    /// <param name="partition">The partition that has recovered.</param>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event the recovery handled, or <see cref="EventSequenceNumber.Unavailable"/> when it handled nothing.</param>
    /// <param name="lastScannedEventSequenceNumber">The event sequence number of the last event the recovery read for the partition, handled or excluded by the observer's filters, or <see cref="EventSequenceNumber.Unavailable"/> when it read nothing.</param>
    /// <param name="episodeId">The recovery episode, or null for a legacy callback.</param>
    /// <param name="deferralId">The deferral that owned the recovery.</param>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task FailedPartitionRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber, FailedPartitionId? episodeId = null, Guid? deferralId = null);

    /// <summary>
    /// Notify that a recovery attempt ended without recovering the partition and without recording a new failure.
    /// </summary>
    /// <param name="partition">The partition that is still failed.</param>
    /// <param name="episodeId">The recovery episode, or null for a legacy callback.</param>
    /// <param name="deferralId">The deferral that owned the recovery.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The retry reminder is removed when the recovery starts, so the partition needs another retry scheduled.
    /// </remarks>
    [AlwaysInterleave]
    Task FailedPartitionNotRecovered(Key partition, FailedPartitionId? episodeId = null, Guid? deferralId = null);

    /// <summary>
    /// Notify that the partition has partially recovered.
    /// </summary>
    /// <param name="partition">The partition that has recovered.</param>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that was handled in the catchup.</param>
    /// <param name="episodeId">The recovery episode, or null for a legacy callback.</param>
    /// <param name="deferralId">The deferral that owned the recovery.</param>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task FailedPartitionPartiallyRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, FailedPartitionId? episodeId = null, Guid? deferralId = null);

    /// <summary>
    /// Check if the observer is currently preparing for a catch-up (between the <see cref="CatchUp"/> call and <see cref="RegisterCatchingUpPartitions"/> being called).
    /// </summary>
    /// <returns>True if preparing for catch-up, false otherwise.</returns>
    [AlwaysInterleave]
    Task<bool> IsPreparingCatchup();

    /// <summary>
    /// Gets whether catch-up is backing off because consecutive catch-up jobs concluded without moving the observer forward.
    /// </summary>
    /// <returns>True if catch-up should not be started again yet, false otherwise.</returns>
    Task<bool> IsCatchUpBackedOff();

    /// <summary>
    /// Check if the observer currently has any failed partitions.
    /// </summary>
    /// <returns>True if there are failed partitions, false otherwise.</returns>
    [AlwaysInterleave]
    Task<bool> HasFailedPartitions();

    /// <summary>
    /// Check if the observer is currently quarantined.
    /// </summary>
    /// <returns>True if observer is quarantined, false otherwise.</returns>
    [AlwaysInterleave]
    Task<bool> IsObserverQuarantined();

    /// <summary>
    /// Get the keys of all currently failed partitions.
    /// </summary>
    /// <returns>Collection of <see cref="Key"/> for failed partitions.</returns>
    [AlwaysInterleave]
    Task<IEnumerable<Key>> GetFailedPartitionKeys();

    /// <summary>
    /// Clears observer quarantine and resumes normal state evaluation.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task ClearObserverQuarantine();

    /// <summary>
    /// Retire and remove this namespace's observer after its operational incidents have been cleared.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Rechecks subscription and activity inside the non-interleaved call. Removal is forward-only and
    /// nontransactional: failure may leave committed retirement or partially deleted resources. Retry continues
    /// cleanup without restoring Active. The source record is deleted last; absent sources are never created.
    /// </remarks>
    Task Remove();

    /// <summary>
    /// Retire the observer: stop it consuming events and end its failures, keeping its records in place.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Used when a projection is retired. Failed partitions and their reminders are discarded and alert incidents
    /// end as Removed. Normal unsubscription disconnects an observing observer, while a quarantined observer
    /// stays quarantined without routing or changing replay progress or handled counts.
    /// The durable Retired disposition suppresses retained quarantine across crashes. A failed reconciliation
    /// fails this call and is retried by the lifetime alert reminder; the manager retains the orphan for retry.
    /// Failure does not undo committed retirement. Automatic subscription recovery leaves it retired.
    /// Explicit registration discards retained episodes before establishing a fresh active lifecycle.
    /// </remarks>
    Task Retire();

    /// <summary>
    /// Catch up the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task CatchUp();

    /// <summary>
    /// Register partitions that the observer is catching up.
    /// </summary>
    /// <param name="partitions">Collection of <see cref="Key">partitions</see>.</param>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task RegisterCatchingUpPartitions(IEnumerable<Key> partitions);

    /// <summary>
    /// Notify that the observer has been caught up.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the catch-up job that has concluded.</param>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that was handled in the catchup.</param>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task CaughtUp(JobId jobId, EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Notify that the observer has been caught up, including how far the catchup read.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that was handled in the catchup.</param>
    /// <param name="lastScannedEventSequenceNumber">The event sequence number of the last event the catchup read, handled or excluded by the observer's filters, or <see cref="EventSequenceNumber.Unavailable"/> when the catchup did not complete every step.</param>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task CaughtUp(EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber);

    /// <summary>
    /// Notify that a specific catch-up job concluded, including how far it read.
    /// </summary>
    /// <param name="jobId">The catch-up job that concluded.</param>
    /// <param name="lastHandledEventSequenceNumber">The last handled event.</param>
    /// <param name="lastScannedEventSequenceNumber">The last scanned event, or unavailable when not every step completed.</param>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task CaughtUp(JobId jobId, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber);

    /// <summary>
    /// Notify that the partition was caught.
    /// </summary>
    /// <param name="partition">The partition that has caught up.</param>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that was handled in the catchup.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionCaughtUp(Key partition, EventSequenceNumber lastHandledEventSequenceNumber);

    /// <summary>
    /// Notify that the partition was caught up, including how far the catchup read.
    /// </summary>
    /// <param name="partition">The partition that has caught up.</param>
    /// <param name="lastHandledEventSequenceNumber">The event sequence number of the last event that was handled in the catchup.</param>
    /// <param name="lastScannedEventSequenceNumber">The event sequence number of the last event the catchup read for the partition, handled or excluded by the observer's filters, or <see cref="EventSequenceNumber.Unavailable"/> when the catchup did not complete.</param>
    /// <returns>Awaitable task.</returns>
    Task PartitionCaughtUp(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber);

    /// <summary>
    /// Attempt to recover a failed partition.
    /// </summary>
    /// <param name="partition">The partition that is failed.</param>
    /// <returns>The <see cref="PartitionRecoveryOutcome"/> describing what happened.</returns>
    Task<PartitionRecoveryOutcome> TryStartRecoverJobForFailedPartition(Key partition);

    /// <summary>
    /// Clear the quarantine of a single failed partition and reset its retry budget, keeping its attempt history.
    /// </summary>
    /// <param name="partition">The quarantined partition.</param>
    /// <param name="retry">Whether to start recovering the partition immediately.</param>
    /// <returns>The <see cref="ClearPartitionQuarantineResult"/> describing what happened.</returns>
    Task<ClearPartitionQuarantineResult> ClearPartitionQuarantine(Key partition, bool retry);

    /// <summary>
    /// Clear all failed partition records for the observer, giving an operator a supported way to recover a wedged
    /// observer without direct storage surgery.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task ClearFailedPartitions();

    /// <summary>
    /// Fast forward the observer to the tail of the event sequence, skipping all events it has not yet handled
    /// without invoking its subscriber.
    /// </summary>
    /// <returns>The <see cref="FastForwardResult"/> describing what happened.</returns>
    Task<FastForwardResult> FastForward();

    /// <summary>
    /// Position a brand new observer at the tail of the event sequence, so that it does not observe history that
    /// predates its first registration.
    /// </summary>
    /// <remarks>
    /// Only an observer that has never been persisted is positioned. One that already has state keeps its position,
    /// which makes the call safe to repeat on every registration. Call it before subscribing.
    /// </remarks>
    /// <returns>True if the observer was new and has been positioned at the tail, false if it already existed.</returns>
    Task<bool> StartAtTailWhenNew();

    /// <summary>
    /// Attempt to recover all failed partitions.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task TryRecoverAllFailedPartitions();

    /// <summary>
    /// Handle events for an <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="partition"><see cref="Key"/> to handle events for.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Handle(Key partition, IEnumerable<AppendedEvent> events);
}
