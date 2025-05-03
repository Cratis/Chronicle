// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents the state of a reactor.
/// </summary>
/// <param name="Id">The unique identifier of the reactor.</param>
/// <param name="RunningState">The current running state of the reactor.</param>
/// <param name="NextEventSequenceNumber">The next event sequence number.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record ReactorState(
    ReactorId Id,
    ObserverRunningState RunningState,
    EventSequenceNumber NextEventSequenceNumber,
    EventSequenceNumber LastHandledEventSequenceNumber);
