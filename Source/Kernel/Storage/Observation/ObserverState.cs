// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the state used for an observer.
/// </summary>
/// <param name="Id">The <see cref="Id"/> representing the observer uniquely.</param>
/// <param name="EventTypes">The event types the observer is observing.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> for the sequence being observed.</param>
/// <param name="Type">The type of observer.</param>
/// <param name="Owner">The owner of the observer.</param>
/// <param name="LastHandledEventSequenceNumber">The <see cref="EventSequenceNumber"/> of the last event the observer handled.</param>
/// <param name="RunningState">The <see cref="ObserverRunningState"/> of the observer.</param>
/// <param name="ReplayingPartitions">The individual partitions that are being replayed.</param>
/// <param name="CatchingUpPartitions">The individual partitions that are catching up.</param>
/// <param name="IsReplaying">Whether the observer is replaying.</param>
/// <param name="IsReplayable">Whether the observer supports replay scenarios.</param>
public record ObserverState(
    ObserverId Id,
    IEnumerable<EventType> EventTypes,
    EventSequenceId EventSequenceId,
    ObserverType Type,
    ObserverOwner Owner,
    EventSequenceNumber LastHandledEventSequenceNumber,
    ObserverRunningState RunningState,
    ISet<Key> ReplayingPartitions,
    ISet<Key> CatchingUpPartitions,
    bool IsReplaying,
    bool IsReplayable)
{
    readonly EventSequenceNumber _nextEventSequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverState"/> class.
    /// </summary>
    public ObserverState()
        : this(
              ObserverId.Unspecified,
              [],
              EventSequenceId.Unspecified,
              ObserverType.Unknown,
              ObserverOwner.None,
              EventSequenceNumber.Unavailable,
              ObserverRunningState.Unknown,
              new HashSet<Key>(),
              new HashSet<Key>(),
              false,
              true)
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
