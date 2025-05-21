// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the current state of an observer.
/// </summary>
/// <param name="RunningState">The current running state of the observer.</param>
/// <param name="IsSubscribed">Indicates whether the observer is subscribed to its handler.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record ObserverState(
    ObserverRunningState RunningState,
    bool IsSubscribed,
    EventSequenceNumber NextEventSequenceNumber,
    EventSequenceNumber LastHandledEventSequenceNumber);