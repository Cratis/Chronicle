// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the state used for an observer.
/// </summary>
/// <param name="Identifier">The <see cref="Identifier"/> representing the observer uniquely.</param>
/// <param name="LastHandledEventSequenceNumber">The <see cref="EventSequenceNumber"/> of the last event the observer handled.</param>
/// <param name="RunningState">The <see cref="ObserverRunningState"/> of the observer.</param>
/// <param name="ReplayingPartitions">The individual partitions that are being replayed.</param>
/// <param name="CatchingUpPartitions">The individual partitions that are catching up.</param>
/// <param name="FailedPartitions">Collection of <see cref="FailedPartition"/>.</param>
/// <param name="FailedPartitionCount">Count of failed partitions for the observer.</param>
/// <param name="IsReplaying">Whether the observer is replaying.</param>
/// <param name="SubscribesToAllEvents">Whether the observer subscribes to all event types.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.ObserverState")]
public record ObserverState(
    [Id(0)] ObserverId Identifier,
    [Id(1)] EventSequenceNumber LastHandledEventSequenceNumber,
    [Id(2)] ObserverRunningState RunningState,
    [Id(3)] ISet<Key> ReplayingPartitions,
    [Id(4)] ISet<Key> CatchingUpPartitions,
    [Id(5)] IEnumerable<FailedPartition> FailedPartitions,
    [Id(6)] FailedPartitionCount FailedPartitionCount,
    [Id(7)] bool IsReplaying,
    [Id(8)] bool SubscribesToAllEvents)
{
    [Id(26)]
    readonly EventSequenceNumber _nextEventSequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverState"/> class.
    /// </summary>
    public ObserverState()
        : this(
              ObserverId.Unspecified,
              EventSequenceNumber.Unavailable,
              ObserverRunningState.Unknown,
              new HashSet<Key>(),
              new HashSet<Key>(),
              [],
              FailedPartitionCount.Zero,
              false,
              false)
    {
    }

    /// <summary>
    /// Gets a new empty observer state.
    /// </summary>
    /// <remarks>
    /// Every access returns a new instance. The partition sets are mutable and owned by the observer that reads the
    /// state, so a shared empty instance would leak partitions from one observer into every other observer that has no
    /// stored state.
    /// </remarks>
    public static ObserverState Empty => new();

    /// <summary>
    /// Gets the owning event sequence, or null for legacy, unowned state.
    /// </summary>
    [Id(9)]
    public EventSequenceId? Owner { get; init; }

    /// <summary>
    /// Gets the partition claims. Shadow (DR 0018 increment 4): written, never read for decisions.
    /// </summary>
    [Id(10)]
    public IEnumerable<PartitionClaim>? PartitionClaims { get; init; }

    /// <summary>
    /// Gets the observer-wide claim. Shadow (DR 0018 increment 4): written, never read for decisions.
    /// </summary>
    [Id(11)]
    public ObserverWideClaim? ObserverWideClaim { get; init; }

    /// <summary>
    /// Gets the ownership revision. Shadow (DR 0018 increment 4): written, never read for decisions.
    /// </summary>
    [Id(12)]
    public OwnershipRevision? OwnershipRevision { get; init; }

    /// <summary>
    /// Gets the namespace observer's type-set control, or null before initialization.
    /// </summary>
    [Id(13)]
    public ObserverTypeSetControl? TypeSetControl { get; init; }

    /// <summary>
    /// Gets the outstanding delivery fences. Null identifies legacy state whose fences must be restored from failures;
    /// an empty snapshot means every fence has been released or expired.
    /// </summary>
    [Id(14)]
    public IEnumerable<DeliveryFenceState>? DeliveryFences { get; init; }

    /// <summary>
    /// Gets the observer's initialization state and latest refusal reasons, or null for legacy state.
    /// </summary>
    [Id(15)]
    public ObserverInitialization? Initialization { get; init; }

    /// <summary>
    /// Gets the source-owned lifecycle token, replaced on subscription but not activation.
    /// </summary>
    [Id(16)]
    public Guid AlertLifecycleId { get; init; }

    /// <summary>
    /// Gets the revision of committed alert-relevant state in this lifecycle.
    /// </summary>
    [Id(17)]
    public long AlertRevision { get; init; }

    /// <summary>
    /// Gets the durable desired activity level used to validate alert reports.
    /// </summary>
    [Id(18)]
    public AlertDisposition AlertDisposition { get; init; }

    /// <summary>
    /// Gets the current quarantine episode identity, or null outside quarantine.
    /// </summary>
    [Id(19)]
    public Guid? QuarantineEpisodeId { get; init; }

    /// <summary>
    /// Gets the source-owned webhook probe control, absent for legacy and non-webhook quarantines.
    /// </summary>
    [Id(20)]
    public WebhookProbeState? WebhookProbe { get; init; }

    /// <summary>
    /// Gets the durable subscription (decision 0034), or null when the observer is not subscribed or has not recorded one.
    /// </summary>
    /// <remarks>
    /// Written in the same state write that records a (re)subscription and cleared when the observer unsubscribes. It is
    /// advisory: it never changes how a resident observer delivers, and a reader validates it before relying on it.
    /// </remarks>
    [Id(21)]
    public ObserverDurableSubscription? DurableSubscription { get; init; }

    /// <summary>
    /// Gets or inits the next <see cref="EventSequenceNumber"/> that the observer is expecting to be handling.
    /// </summary>
    public EventSequenceNumber NextEventSequenceNumber
    {
        get => _nextEventSequenceNumber;
        init => _nextEventSequenceNumber = !value.IsActualValue ? EventSequenceNumber.First : value;
    }

    /// <summary>
    /// Gets or inits the tail <see cref="EventSequenceNumber"/> for the observed event sequence.
    /// </summary>
    [Id(22)]
    public EventSequenceNumber TailEventSequenceNumber
    {
        get;
        init;
    }
        = EventSequenceNumber.Unavailable;

    /// <summary>
    /// Gets or inits the partitions that have an in-flight event batch — one the observer has started handling
    /// but not yet acknowledged.
    /// </summary>
    /// <remarks>
    /// A partition is recorded here, and the state made durable, immediately before its subscriber is invoked, and
    /// removed once the outcome of that invocation is known. Persisting the markers as part of the observer state
    /// (rather than in a separate write) lets a crash that interrupted handling recover the affected partitions on
    /// the next activation without an extra round trip per handled batch. The marker's only purpose is this
    /// durability ordering relative to the subscriber call.
    /// </remarks>
    [Id(23)]
    public ISet<Key> InFlightPartitions { get; init; } = new HashSet<Key>();

    /// <summary>
    /// Gets or inits the total number of events the observer has successfully handled.
    /// </summary>
    [Id(24)]
    public EventCount HandledEventCount { get; init; } = EventCount.Zero;

    /// <summary>
    /// Gets or inits the number of events successfully handled, broken down by event type identifier.
    /// </summary>
    [Id(25)]
    public IReadOnlyDictionary<EventTypeId, EventCount> HandledEventCountPerEventType { get; set; } = ImmutableDictionary<EventTypeId, EventCount>.Empty;

    /// <summary>Gets the outstanding durable partition repairs, when any are owed.</summary>
    [Id(27)]
    public PartitionRepairState[]? PartitionRepairs { get; init; }

    /// <summary>
    /// Gets or sets the number of events successfully handled, broken down by event type identifier.
    /// Kept for backwards compatibility with integration tests and benchmarks using the previous property name.
    /// </summary>
    [Obsolete($"Use {nameof(HandledEventCountPerEventType)} instead.")]
    public IReadOnlyDictionary<EventTypeId, EventCount> HandledEventTypesCount
    {
        get => HandledEventCountPerEventType;
        set => HandledEventCountPerEventType = value;
    }
}
