// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents the state of a reducer.
/// </summary>
/// <param name="RunningState">The current running state of the projection.</param>
/// <param name="IsSubscribed">Indicates whether the projection is subscribed its handler.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
[Obsolete("To not break with code this can be marked as obsolete for now until we figure out whether to have more specific info here")]
public record ProjectionState(
    ObserverRunningState RunningState,
    bool IsSubscribed,
    EventSequenceNumber NextEventSequenceNumber,
    EventSequenceNumber LastHandledEventSequenceNumber) : ObserverState(RunningState, IsSubscribed, NextEventSequenceNumber, LastHandledEventSequenceNumber);
