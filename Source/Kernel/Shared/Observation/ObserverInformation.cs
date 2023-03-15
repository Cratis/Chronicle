// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Observation;

/// <summary>
/// Represents information about an observer.
/// </summary>
/// <param name="ObserverId">The unique identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer is observing.</param>
/// <param name="Name">Name of the observer.</param>
/// <param name="Type">Type of observer.</param>
/// <param name="EventTypes">Types of events the observer is observing.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number the observer will observe.</param>
/// <param name="RunningState">The running state of the observer.</param>
public record ObserverInformation(
    ObserverId ObserverId,
    EventSequenceId EventSequenceId,
    ObserverName Name,
    ObserverType Type,
    IEnumerable<EventType> EventTypes,
    EventSequenceNumber NextEventSequenceNumber,
    ObserverRunningState RunningState);
