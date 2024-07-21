// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactions;

/// <summary>
/// Represents information about an observer.
/// </summary>
/// <param name="ObserverId">The unique identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer is observing.</param>
/// <param name="Name">Name of the observer.</param>
/// <param name="Type">Type of observer.</param>
/// <param name="EventTypes">Types of events the observer is observing.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number the observer will observe.</param>
/// <param name="LastHandledEventSequenceNumber">The event sequence number the observer last handled.</param>
/// <param name="RunningState">The running state of the observer.</param>
/// <param name="Handled">Number of events handled by the observer.</param>
/// <param name="FailedPartitions">Collection of <see cref="FailedPartition"/>.</param>
public record ObserverInformation(
    ObserverId ObserverId,
    EventSequenceId EventSequenceId,
    ObserverName Name,
    ObserverType Type,
    IEnumerable<EventType> EventTypes,
    EventSequenceNumber NextEventSequenceNumber,
    EventSequenceNumber LastHandledEventSequenceNumber,
    ObserverRunningState RunningState,
    EventCount Handled,
    IEnumerable<FailedPartition> FailedPartitions);
