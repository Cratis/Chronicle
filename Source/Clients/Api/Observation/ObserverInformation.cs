// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.Events;

namespace Cratis.Chronicle.Api.Observation;

/// <summary>
/// Represents the information about an observer.
/// </summary>
/// <param name="Id">The unique identifier of the observer.</param>
/// <param name="EventSequenceId">The event sequence the observer is observing.</param>
/// <param name="Type">The type of observer.</param>
/// <param name="EventTypes">The types of events the observer is observing.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number the observer will observe.</param>
/// <param name="LastHandledEventSequenceNumber">The event sequence number the observer last handled.</param>
/// <param name="RunningState">The running state of the observer.</param>
/// <param name="IsSubscribed">A value indicating whether the observer is subscribed to its handler.</param>
public record ObserverInformation(
    string Id,
    string EventSequenceId,
    ObserverType Type,
    IEnumerable<EventType> EventTypes,
    ulong NextEventSequenceNumber,
    ulong LastHandledEventSequenceNumber,
    ObserverRunningState RunningState,
    bool IsSubscribed);
