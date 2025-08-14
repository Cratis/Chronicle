// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;
#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents the state of an observer.
/// </summary>
/// <param name="Id">The unique identifier of the observer.</param>
/// <param name="LastHandledEventSequenceNumber">The last handled event sequence number.</param>
/// <param name="RunningState">The current running state of the observer.</param>
/// <param name="ReplayingPartitions">The set of partitions being replayed.</param>
/// <param name="CatchingUpPartitions">The set of partitions being caught up.</param>
/// <param name="IsReplaying">Indicates if the observer is currently replaying events.</param>
/// <param name="IsReplayable">Indicates if the observer can be replayed.</param>
public record ObserverState(
    ObserverId Id,
    EventSequenceNumber LastHandledEventSequenceNumber,
    ObserverRunningState RunningState,
    ISet<Key> ReplayingPartitions,
    ISet<Key> CatchingUpPartitions,
    bool IsReplaying,
    bool IsReplayable);
