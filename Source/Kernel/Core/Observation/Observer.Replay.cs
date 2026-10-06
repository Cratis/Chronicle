// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.StateMachines;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task<JobId> Replay()
    {
        ThrowIfSealed();
        if (IsRetired || !Definition.IsReplayable)
        {
            return JobId.NotSet;
        }

        if (State.RunningState != ObserverRunningState.Replaying && await CanTransitionTo<Replay>())
        {
            // A transition requested while another one is in progress - typically one driven by an interleaved
            // callback such as CaughtUp - is only scheduled, and the state being entered may replace it with its own
            // next state. The request is therefore held until a replay actually starts, or until the observer
            // settles somewhere it can not replay from, so the caller gets the job that is really replaying.
            var pendingReplay = _pendingReplay ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
            await TransitionTo<Replay>();
            if (!pendingReplay.Task.IsCompleted)
            {
                logger.ReplayDeferred();
            }

            try
            {
                return await pendingReplay.Task.WaitAsync(_pendingReplayTimeout);
            }
            catch (TimeoutException)
            {
                logger.RequestedReplayDidNotStart();
                ConcludePendingReplay(pendingReplay, JobId.NotSet);
                return JobId.NotSet;
            }
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
    public Task Replayed(EventSequenceNumber lastHandledEventSequenceNumber) => CompleteReplay(lastHandledEventSequenceNumber, new Dictionary<Key, EventSequenceNumber>(), [], DateTimeOffset.MinValue);

    /// <inheritdoc/>
    public Task ReplayedSuccessfullySince(EventSequenceNumber lastHandledEventSequenceNumber, IReadOnlyDictionary<Key, EventSequenceNumber> replayedPartitions, EventType[] replayedEventTypes, DateTimeOffset replayStartedAt) =>
        CompleteReplay(lastHandledEventSequenceNumber, replayedPartitions, replayedEventTypes, replayStartedAt);

    /// <inheritdoc/>
    public Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber) => CompletePartitionReplay(partition, lastHandledEventSequenceNumber, []);

    /// <inheritdoc/>
    public Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventType[] replayedEventTypes) =>
        CompletePartitionReplay(partition, lastHandledEventSequenceNumber, replayedEventTypes);

    /// <inheritdoc/>
    public Task PartitionReplayPartiallyCompleted(Key partition, EventSequenceNumber lastHandledEventSequenceNumber) => CompletePartitionReplay(partition, lastHandledEventSequenceNumber, []);

    /// <inheritdoc/>
    protected override async Task OnAfterEnteringState(IState<ObserverState> state)
    {
        if (_pendingReplay is not { } pendingReplay) return;

        switch (state)
        {
            case Replay replay:
                ConcludePendingReplay(pendingReplay, replay.LastStartedJobId);
                break;

            // The observer settled without replaying - the scheduled replay was replaced by the next state of the
            // transition that was in progress. It is still in a state that can replay, so replay from here; this
            // runs as part of the ongoing transition and is therefore performed as soon as entering it completes.
            case Observing:
                await TransitionTo<Replay>();
                break;

            case Disconnected:
            case QuarantinedObserver:
                ConcludePendingReplay(pendingReplay, JobId.NotSet);
                break;
        }
    }

    void ConcludePendingReplay(TaskCompletionSource<JobId> pendingReplay, JobId jobId)
    {
        if (ReferenceEquals(_pendingReplay, pendingReplay))
        {
            _pendingReplay = null;
        }

        pendingReplay.TrySetResult(jobId);
    }

    async Task CompleteReplay(EventSequenceNumber lastHandledEventSequenceNumber, IReadOnlyDictionary<Key, EventSequenceNumber> replayedPartitions, EventType[] replayedEventTypes, DateTimeOffset replayStartedAt)
    {
        if (IsRetired || _removed) return;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        var eventTypeIds = replayedEventTypes.Select(_ => _.Id).ToHashSet();
        if (eventTypeIds.Count > 0 && replayedPartitions.Count > 0)
        {
            var eventSequenceStorage = storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).GetEventSequence(_observerKey.EventSequenceId);
            foreach (var failedPartition in Failures.Partitions.ToArray())
            {
                if (!replayedPartitions.TryGetValue(failedPartition.Partition, out var handledTo) ||
                    !handledTo.IsActualValue || failedPartition.LastAttempt.SequenceNumber > handledTo ||
                    failedPartition.LastAttempt.Occurred > replayStartedAt)
                {
                    continue;
                }

                // The failure record stores a sequence number, not an event type. Check the stored event itself:
                // a newer event in this partition (or in another partition) does not prove the failed type was read.
                AppendedEvent failedEvent;
                try
                {
                    failedEvent = await eventSequenceStorage.GetEventAt(failedPartition.LastAttempt.SequenceNumber);
                }
                catch (Exception exception)
                {
                    logger.FailedLookingUpReplayFailure(exception, failedPartition.Partition, failedPartition.LastAttempt.SequenceNumber);
                    continue;
                }

                if (failedEvent.Context.SequenceNumber == failedPartition.LastAttempt.SequenceNumber &&
                    failedEvent.Context.EventSourceId == failedPartition.Partition.ToString() &&
                    eventTypeIds.Contains(failedEvent.Context.EventType.Id))
                {
                    await ResolveFailedPartition(failedPartition.Partition);
                }
            }
        }

        State = State with
        {
            IsReplaying = false,
            LastHandledEventSequenceNumber = lastHandledEventSequenceNumber,
            NextEventSequenceNumber = lastHandledEventSequenceNumber == EventSequenceNumber.Unavailable ? EventSequenceNumber.First : lastHandledEventSequenceNumber.Next()
        };
        await WriteStateAsync();
        await TransitionTo<Routing>();
    }

    async Task CompletePartitionReplay(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventType[] replayedEventTypes)
    {
        if (IsRetired || _removed) return;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.FinishedReplayForPartition(partition);
        State.ReplayingPartitions.Remove(partition);
        if (replayedEventTypes.Length > 0 && lastHandledEventSequenceNumber.IsActualValue &&
            Failures.TryGet(partition, out var failure) && failure.LastAttempt.SequenceNumber <= lastHandledEventSequenceNumber)
        {
            var eventSequenceStorage = storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).GetEventSequence(_observerKey.EventSequenceId);
            AppendedEvent? failedEvent = null;
            try
            {
                failedEvent = await eventSequenceStorage.GetEventAt(failure.LastAttempt.SequenceNumber);
            }
            catch (Exception exception)
            {
                logger.FailedLookingUpReplayFailure(exception, partition, failure.LastAttempt.SequenceNumber);
            }

            if (failedEvent is not null && failedEvent.Context.SequenceNumber == failure.LastAttempt.SequenceNumber &&
                failedEvent.Context.EventSourceId == partition.ToString() && replayedEventTypes.Any(_ => _.Id == failedEvent.Context.EventType.Id))
            {
                await ResolveFailedPartition(partition);
            }
        }

        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
        await StartCatchupJobIfNeeded(partition, lastHandledEventSequenceNumber);
    }

    async Task ReplayPartitionTo(Key partition, EventSequenceNumber sequenceNumber, IEnumerable<EventType> eventTypes, bool retainOtherCounts)
    {
        ThrowIfSealed();
        if (IsRetired || !Definition.IsReplayable)
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
        await _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(new(_observerKey, Definition.Type, partition, EventSequenceNumber.First, sequenceNumber, eventTypesToReplay)
        {
            ReplaysAllEventTypes = !retainOtherCounts
        });

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
