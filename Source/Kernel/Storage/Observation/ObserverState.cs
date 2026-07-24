// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
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
public record ObserverState(
    ObserverId Identifier,
    EventSequenceNumber LastHandledEventSequenceNumber,
    ObserverRunningState RunningState,
    ISet<Key> ReplayingPartitions,
    ISet<Key> CatchingUpPartitions,
    IEnumerable<FailedPartition> FailedPartitions,
    FailedPartitionCount FailedPartitionCount,
    bool IsReplaying,
    bool SubscribesToAllEvents)
{
    /// <summary>
    /// Represents an empty observer state.
    /// </summary>
    public static readonly ObserverState Empty = new();

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
    public ISet<Key> InFlightPartitions { get; init; } = new HashSet<Key>();

    /// <summary>
    /// Gets or inits the total number of events the observer has successfully handled.
    /// </summary>
    public EventCount HandledEventCount { get; init; } = EventCount.Zero;

    /// <summary>
    /// Gets or inits the number of events successfully handled, broken down by event type identifier.
    /// </summary>
    public IReadOnlyDictionary<EventTypeId, EventCount> HandledEventCountPerEventType { get; set; } = ImmutableDictionary<EventTypeId, EventCount>.Empty;

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
