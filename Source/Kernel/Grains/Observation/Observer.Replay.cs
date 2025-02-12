// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Grains.Observation.States;
namespace Cratis.Chronicle.Grains.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task Replay()
    {
        if (State.RunningState == ObserverRunningState.Active)
        {
            await TransitionTo<Replay>();
        }
    }

    /// <inheritdoc/>
    public Task ReplayPartition(Key partition) => ReplayPartitionTo(partition, EventSequenceNumber.Max);

    /// <inheritdoc/>
    public async Task ReplayPartitionTo(Key partition, EventSequenceNumber sequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.AttemptReplayPartition(partition, sequenceNumber);
        await _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(
            JobId.New(),
            new(
                _observerKey,
                _subscription,
                partition,
                EventSequenceNumber.First,
                sequenceNumber,
                State.EventTypes));

        State.ReplayingPartitions.Add(partition);
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task Replayed(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
        await TransitionTo<Routing>();
    }

    /// <inheritdoc/>
    public async Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.FinishedReplayForPartition(partition);
        State.ReplayingPartitions.Remove(partition);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
        await StartCatchupJobIfNeeded(partition, lastHandledEventSequenceNumber);
    }
}
