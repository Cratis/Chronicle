// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the state used for an observer.
/// </summary>
/// <param name="EventTypes">The event types the observer is observing.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> for the sequence being observed.</param>
/// <param name="ObserverId">The <see cref="ObserverId"/> representing the observer uniquely.</param>
/// <param name="Type">The type of observer.</param>
/// <param name="NextEventSequenceNumberForEventTypes">The next <see cref="EventSequenceNumber"/> for the event types the observer is for.</param>
/// <param name="LastHandledEventSequenceNumber">The <see cref="EventSequenceNumber"/> of the last event the observer handled.</param>
/// <param name="Handled">Number of events that has been handled by the observer.</param>
/// <param name="RunningState">The <see cref="ObserverRunningState"/> of the observer.</param>
public record ObserverState(
    IEnumerable<EventType> EventTypes,
    EventSequenceId EventSequenceId,
    ObserverId ObserverId,
    ObserverType Type,
    EventSequenceNumber NextEventSequenceNumberForEventTypes,
    EventSequenceNumber LastHandledEventSequenceNumber,
    EventCount Handled,
    ObserverRunningState RunningState)
{
    EventSequenceNumber _nextEventSequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverState"/> class.
    /// </summary>
    public ObserverState()
        : this(
              [],
              EventSequenceId.Unspecified,
              ObserverId.Unspecified,
              ObserverType.Unknown,
              EventSequenceNumber.Unavailable,
              EventSequenceNumber.Unavailable,
              EventCount.NotSet,
              ObserverRunningState.New)
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
