// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task<JobId> Replay()
    {
        if (!Definition.IsReplayable)
        {
            return JobId.NotSet;
        }

        if (State.RunningState != ObserverRunningState.Replaying)
        {
            await TransitionTo<Replay>();
        }

        var states = await GetStates();
        var replayState = states.OfType<Replay>().FirstOrDefault();
        return replayState?.LastStartedJobId ?? JobId.NotSet;
    }

    /// <inheritdoc/>
    public Task ReplayPartition(Key partition) => ReplayPartitionTo(partition, EventSequenceNumber.Max, Definition.EventTypes, false);

    /// <inheritdoc/>
    public Task ReplayPartition(Key partition, IEnumerable<EventType> eventTypes) =>
        ReplayPartitionTo(partition, EventSequenceNumber.Max, eventTypes, true);

    /// <inheritdoc/>
    public Task ReplayPartitionTo(Key partition, EventSequenceNumber sequenceNumber) =>
        ReplayPartitionTo(partition, sequenceNumber, Definition.EventTypes, false);

    /// <inheritdoc/>
    public async Task Replayed(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        State = State with
        {
            IsReplaying = false,
            LastHandledEventSequenceNumber = lastHandledEventSequenceNumber,
            NextEventSequenceNumber = lastHandledEventSequenceNumber == EventSequenceNumber.Unavailable ? EventSequenceNumber.First : lastHandledEventSequenceNumber.Next()
        };
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

    async Task ReplayPartitionTo(Key partition, EventSequenceNumber sequenceNumber, IEnumerable<EventType> eventTypes, bool retainOtherCounts)
    {
        if (!Definition.IsReplayable)
        {
            return;
        }

        if (State.RunningState == ObserverRunningState.Replaying)
        {
            logger.SkippingPartitionReplayBecauseObserverIsReplaying();
            return;
        }

        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.AttemptReplayPartition(partition, sequenceNumber);

        var eventTypesToReplay = eventTypes.ToArray();
        var eventTypeIdsToReplay = eventTypesToReplay.Select(_ => _.Id).ToHashSet();
        var handledCountsStorage = GetObserverHandledCountsStorage();
        var partitionCounts = await handledCountsStorage.GetFor(_observerId, partition);
        var replayedPartitionCounts = retainOtherCounts
            ? partitionCounts.Where(_ => eventTypeIdsToReplay.Contains(_.Key)).ToDictionary()
            : partitionCounts.ToDictionary();
        State = WithSubtractedPartitionHandledEventCounts(State, replayedPartitionCounts);
        await handledCountsStorage.RemoveFor(_observerId, partition);
        var retainedPartitionCounts = retainOtherCounts
            ? partitionCounts.Where(_ => !eventTypeIdsToReplay.Contains(_.Key)).ToDictionary()
            : new Dictionary<EventTypeId, EventCount>();
        if (retainedPartitionCounts.Count > 0)
        {
            await handledCountsStorage.Increment(_observerId, partition, retainedPartitionCounts);
        }
        await _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(new(_observerKey, Definition.Type, partition, EventSequenceNumber.First, sequenceNumber, eventTypesToReplay));

        State.ReplayingPartitions.Add(partition);
        await WriteStateAsync();
    }

    async Task<bool> TransitionToReplayIfNeeded()
    {
        if (State.RunningState == ObserverRunningState.Replaying)
        {
            logger.Replaying();
            await TransitionTo<Replay>();
            return true;
        }

        var tailSequenceNumber = await _eventSequence.GetTailSequenceNumber();
        var getNextToHandleResult = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(State.NextEventSequenceNumber, _subscription.EventTypes.ToList());
        var nextUnhandledEventSequenceNumber = getNextToHandleResult.Match(eventSequenceNumber => eventSequenceNumber, _ => EventSequenceNumber.Unavailable);
        var replayEvaluator = new ReplayEvaluator(GrainFactory, _subscription.ObserverKey.EventStore, _observerKey.Namespace);
        if (!await replayEvaluator.Evaluate(new(
                State.Identifier,
                _subscription.ObserverKey,
                Definition,
                State,
                _subscription,
                tailSequenceNumber,
                nextUnhandledEventSequenceNumber)))
        {
            return false;
        }

        logger.NeedsToReplay();
        await TransitionTo<Replay>();
        return true;
    }
}
