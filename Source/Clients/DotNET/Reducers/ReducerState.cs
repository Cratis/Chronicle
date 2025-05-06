// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents the state of a reducer.
/// </summary>
/// <param name="RunningState">The current running state of the reducer.</param>
/// <param name="IsSubscribed">Indicates whether the reducer is subscribed its handler.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record ReducerState(
    ObserverRunningState RunningState,
    bool IsSubscribed,
    EventSequenceNumber NextEventSequenceNumber,
    EventSequenceNumber LastHandledEventSequenceNumber);
