// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
/// <param name="IsReplaying">Whether the observer is replaying.</param>
/// <param name="SubscribesToAllEvents">Whether the observer subscribes to all event types.</param>
public record ObserverState(
    ObserverId Identifier,
    EventSequenceNumber LastHandledEventSequenceNumber,
    ObserverRunningState RunningState,
    ISet<Key> ReplayingPartitions,
    ISet<Key> CatchingUpPartitions,
    IEnumerable<FailedPartition> FailedPartitions,
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
}
