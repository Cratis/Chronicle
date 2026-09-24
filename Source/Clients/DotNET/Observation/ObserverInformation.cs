// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents what the event store knows about one of its observers.
/// </summary>
/// <param name="Id">The <see cref="ObserverId"/> identifying the observer.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the observer observes.</param>
/// <param name="Type">The <see cref="ObserverType"/> of the observer.</param>
/// <param name="RunningState">The <see cref="ObserverRunningState"/> the observer is in.</param>
/// <param name="LastHandledEventSequenceNumber">The <see cref="EventSequenceNumber"/> of the last event handled.</param>
/// <param name="NextEventSequenceNumber">The next <see cref="EventSequenceNumber"/> the observer expects to handle.</param>
/// <param name="HandledEventCount">The total number of events the observer has handled.</param>
public record ObserverInformation(
    ObserverId Id,
    EventSequenceId EventSequenceId,
    ObserverType Type,
    ObserverRunningState RunningState,
    EventSequenceNumber LastHandledEventSequenceNumber,
    EventSequenceNumber NextEventSequenceNumber,
    ulong HandledEventCount);
