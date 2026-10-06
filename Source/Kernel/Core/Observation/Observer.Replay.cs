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
            // next state. Waiting for the replay state to be entered would hold this call open while that
            // transition runs, and anything it calls back into the observer would queue behind it. The replay job is
            // therefore started here, so its id is known up front; entering the replay state adopts the running job.
            var jobId = await StartOrResumeReplayJob();

            // The request stays raised until the replay state is entered, so a scheduled replay replaced by the
            // state being entered is entered once the observer settles in Observing - see OnAfterEnteringState.
            _replayRequested = true;
            try
            {
                await TransitionTo<Replay>();
            }
            catch
            {
                _replayRequested = false;
                throw;
            }

            if (State.RunningState != ObserverRunningState.Replaying)
            {
                logger.ReplayDeferred();
                return jobId;
            }

            _replayRequested = false;
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
        if (!_replayRequested) return;

        switch (state)
        {
            // The observer settled without replaying - the scheduled replay was replaced by the next state of the
            // transition that was in progress. It is still in a state that can replay, so replay from here; this
            // runs as part of the ongoing transition and is therefore performed as soon as entering it completes.
            case Observing:
                await TransitionTo<Replay>();
                break;

            case Disconnected:
            case QuarantinedObserver:
                _replayRequested = false;
                break;
        }
    }

    Task<JobId> StartOrResumeReplayJob() =>
        _jobsManager.StartOrResumeObserverJobFor<IReplayObserver, ReplayObserverRequest>(
            logger,

            // Routing hands the subscribed event types to the definition as it leaves, before the replay state is
            // entered, so the request is made with the event types the replay state would use.
            new(_observerKey, Definition.Type, _subscription.IsSubscribed ? _subscription.EventTypes : Definition.EventTypes));

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

        // The replay has run, so a request for it that never got the observer into the replay state is moot.
        _replayRequested = false;
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
