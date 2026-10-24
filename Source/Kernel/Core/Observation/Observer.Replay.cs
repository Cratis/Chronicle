// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;
namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    /// <remarks>
    /// A transition requested while another is in progress is only scheduled, and a later request replaces it. The only
    /// call that can be transitioning while this one runs is an interleaving <see cref="CaughtUp(JobId, EventSequenceNumber)"/>, whose routing then
    /// picks the next state itself and so silently replaced the replay. Waiting for those handovers to settle lets the
    /// replay transition run here, in this call, so the job id returned is the one this call started or resumed.
    /// </remarks>
    public async Task<JobId> Replay()
    {
        ThrowIfSealed();
        if (IsRetired || !Definition.IsReplayable)
        {
            return JobId.NotSet;
        }

        // Refuse before resetting progress, entering replay, or starting a job. The handler also guards
        // persisted jobs that resume without coming through this observer entry point.
        await GlobalProjectionReplay.EnsureSupported(storage, _observerKey, Definition.Type);

        while (_catchUpHandoversSettled is { } settled)
        {
            logger.WaitingForCatchUpHandoverBeforeReplay();
            await settled.Task;
        }

        if (IsRetired || _removed || !Definition.IsReplayable)
        {
            return JobId.NotSet;
        }

        // Nothing from here until the transition has marked itself in progress may yield: CanTransitionTo completes
        // synchronously and the transition is marked in progress before its first await, so no handover can start
        // routing in between and replace it.
        if (State.RunningState != ObserverRunningState.Replaying)
        {
            Skips.Reset();
            var canReplay = await CanTransitionTo<Replay>();
            await TransitionTo<Replay>();
            if (State.RunningState != ObserverRunningState.Replaying)
            {
                if (canReplay)
                {
                    logger.ReplayTransitionDidNotTakeEffect();
                }

                return JobId.NotSet;
            }
        }

        var states = await GetStates();
        return states.OfType<Replay>().First().LastStartedJobId;
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
    public Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber) =>
        RecordPartitionReplayCompletion(new(partition, null) { LastHandledEventSequenceNumber = lastHandledEventSequenceNumber });

    /// <inheritdoc/>
    public Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventType[] replayedEventTypes) =>
        RecordPartitionReplayCompletion(new(partition, null) { LastHandledEventSequenceNumber = lastHandledEventSequenceNumber, ReplayedEventTypes = replayedEventTypes });

    /// <inheritdoc/>
    public Task PartitionReplayPartiallyCompleted(Key partition, EventSequenceNumber lastHandledEventSequenceNumber) =>
        RecordPartitionReplayCompletion(new(partition, null) { LastHandledEventSequenceNumber = lastHandledEventSequenceNumber });

    /// <inheritdoc/>
    public Task PartitionReplayed(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber, EventType[] replayedEventTypes) =>
        RecordPartitionReplayCompletion(new(partition, null)
        {
            LastHandledEventSequenceNumber = lastHandledEventSequenceNumber,
            LastScannedEventSequenceNumber = lastScannedEventSequenceNumber,
            ReplayedEventTypes = replayedEventTypes
        });

    [LoggerMessage(LogLevel.Warning, "Could not flush handled-event counts while completing replay; counts remain pending")]
    static partial void FailedFlushingHandledCountsOnReplay(ILogger logger, Exception exception);

    async Task CompleteReplay(EventSequenceNumber lastHandledEventSequenceNumber, IReadOnlyDictionary<Key, EventSequenceNumber> replayedPartitions, EventType[] replayedEventTypes, DateTimeOffset replayStartedAt)
    {
        if (IsRetired || _removed) return;
        try
        {
            await FlushPendingHandledCounts();
        }
        catch (Exception exception)
        {
            FailedFlushingHandledCountsOnReplay(logger, exception);
        }

        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        Skips.Reset();

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
            PartitionReplayCompletions = [],
            LastHandledEventSequenceNumber = lastHandledEventSequenceNumber,
            NextEventSequenceNumber = lastHandledEventSequenceNumber == EventSequenceNumber.Unavailable ? EventSequenceNumber.First : lastHandledEventSequenceNumber.Next()
        };
        State.CatchingUpPartitions.Clear();
        State.ReplayingPartitions.Clear();
        await WriteStateAsync();
        if (!IsQuarantined)
        {
            await TransitionTo<Routing>();
        }
    }

    /// <summary>
    /// Ends a partition replay, whether or not it handled anything.
    /// </summary>
    /// <param name="partition">The partition that was replayed.</param>
    /// <param name="lastHandledEventSequenceNumber">The last event the replay handled, or <see cref="EventSequenceNumber.Unavailable"/>.</param>
    /// <param name="replayedEventTypes">The event types the replay proves it handled, or empty.</param>
    /// <param name="lastScannedEventSequenceNumber">The last event the replay read, handled or excluded by the observer's filters.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Live delivery holds back a partition while it is replaying, so the replay marker must come down however the
    /// replay ended - including a replay that read only events the observer's filters exclude and so handled nothing.
    /// Events held back while it replayed are handed to catch-up from the furthest event the replay read.
    /// </remarks>
    async Task PreparePartitionReplayCompletion(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventType[] replayedEventTypes, EventSequenceNumber lastScannedEventSequenceNumber)
    {
        if (IsRetired || _removed) return;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        Skips.Completing(partition, FurthestOf(lastHandledEventSequenceNumber, lastScannedEventSequenceNumber));
        logger.FinishedReplayForPartition(partition);
        State.CatchingUpPartitions.Add(partition);
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

        HandleNewLastHandledEventForPartition(partition, lastHandledEventSequenceNumber);
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

        await GlobalProjectionReplay.EnsureSupported(storage, _observerKey, Definition.Type);
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
        RecordedHandlingState();
        await handledCountsStorage.RemoveFor(_observerId, partition);
        var retainedPartitionCounts = retainOtherCounts
            ? partitionCounts.Where(_ => !eventTypeIdsToReplay.Contains(_.Key)).ToDictionary()
            : new Dictionary<EventTypeId, EventCount>();
        if (retainedPartitionCounts.Count > 0)
        {
            await handledCountsStorage.Increment(_observerId, partition, retainedPartitionCounts);
        }
        var addedFence = State.ReplayingPartitions.Add(partition);
        await WriteStateAsync();
        try
        {
            var started = await _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(new(_observerKey, Definition.Type, partition, EventSequenceNumber.First, sequenceNumber, eventTypesToReplay)
            {
                ReplaysAllEventTypes = !retainOtherCounts,
                ReaderScope = ReaderScope
            });
            if (started.IsSuccess)
            {
                Skips.JobStarted(partition);
                return;
            }
        }
        catch
        {
            await RollBackPartitionReplayFence(partition, addedFence);
            throw;
        }

        await RollBackPartitionReplayFence(partition, addedFence);
    }

    async Task<bool> TransitionToReplayIfNeeded(int recovery)
    {
        if (IsRecoverySuperseded(recovery))
        {
            return true;
        }

        if (State.RunningState == ObserverRunningState.Replaying)
        {
            logger.Replaying();
            await TransitionTo<Replay>();
            return true;
        }

        var tailSequenceNumber = await _eventSequence.GetTailSequenceNumber();
        if (IsRecoverySuperseded(recovery))
        {
            return true;
        }

        var getNextToHandleResult = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(State.NextEventSequenceNumber, _subscription.EventTypes.ToList());
        if (IsRecoverySuperseded(recovery))
        {
            return true;
        }
        var nextUnhandledEventSequenceNumber = getNextToHandleResult.Match(eventSequenceNumber => eventSequenceNumber, _ => EventSequenceNumber.Unavailable);
        var replayEvaluator = new ReplayEvaluator(GrainFactory, _subscription.ObserverKey.EventStore, _observerKey.Namespace);
        var needsReplay = await replayEvaluator.Evaluate(new(
            State.Identifier,
            _subscription.ObserverKey,
            Definition,
            State,
            _subscription,
            tailSequenceNumber,
            nextUnhandledEventSequenceNumber));
        if (IsRecoverySuperseded(recovery))
        {
            return true;
        }

        if (!needsReplay)
        {
            return false;
        }

        logger.NeedsToReplay();
        await TransitionTo<Replay>();
        return true;
    }
}
