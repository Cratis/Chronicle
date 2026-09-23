// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task CatchUp()
    {
        _isPreparingCatchup = true;
        using var scope = logger.BeginObserverScope(State.Identifier, _observerKey);

        if (State.RunningState == ObserverRunningState.Replaying)
        {
            logger.SkippingCatchUpBecauseObserverIsReplaying();
            _isPreparingCatchup = false;
            return;
        }

        var subscription = await GetSubscription();
        await _jobsManager.StartOrResumeObserverJobFor<ICatchUpObserver, CatchUpObserverRequest>(
            logger,
            new(_observerKey, Definition.Type, State.NextEventSequenceNumber, subscription.EventTypes),
            requestPredicate: null,
            () =>
            {
                logger.FinishingExistingCatchUpJob();
                return Task.CompletedTask;
            },
            () =>
            {
                logger.ResumingCatchUpJob();
                return Task.CompletedTask;
            },
            () =>
            {
                logger.StartCatchUpJob(State.NextEventSequenceNumber);
                return Task.CompletedTask;
            });
    }

    /// <inheritdoc/>
    public async Task RegisterCatchingUpPartitions(IEnumerable<Key> partitions)
    {
        using var scope = logger.BeginObserverScope(State.Identifier, _observerKey);
        logger.RegisteringCatchingUpPartitions();
        foreach (var partition in partitions)
        {
            State.CatchingUpPartitions.Add(partition);
        }

        await WriteStateAsync();

        _isPreparingCatchup = false;

        // Reached only when a brand-new job's steps actually prepared - genuine forward progress, so the
        // consecutive-stranding count the watchdog uses to decide when to give up resets with it.
        _catchupRecoveryAttempts = 0;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Catch-up is over however it got here, so the preparing flag comes down with it. Lowering it only in
    /// <see cref="RegisterCatchingUpPartitions"/> covers just the path where a brand-new job prepared steps.
    /// A job that was already running, one that was resumed, and one found with every step already completed
    /// and finalized rather than resumed all reach completion without preparing steps a second time, and each
    /// of them left the flag raised for the lifetime of the activation - which makes <c language="csharp">Handle</c> drop every
    /// live event and makes <see cref="States.Observing"/> skip its missed-events check, so the observer never
    /// observes anything again. The watchdog then clears the flag, routes, and catch-up concludes the same way
    /// on the next tick, five times over, until the observer is quarantined for a strand that was never its
    /// own fault.
    /// </remarks>
    public async Task CaughtUp(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();

        _isPreparingCatchup = false;
        _catchupRecoveryAttempts = 0;

        await TransitionTo<Routing>();
    }

    /// <inheritdoc/>
    public async Task PartitionCaughtUp(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.PartitionCaughtUp(partition, lastHandledEventSequenceNumber);
        State.CatchingUpPartitions.Remove(partition);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
        await StartCatchupJobIfNeeded(partition, lastHandledEventSequenceNumber);
    }

    async Task StartCatchupJobIfNeeded(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        if (State.RunningState == ObserverRunningState.Replaying)
        {
            logger.SkippingPartitionCatchUpBecauseObserverIsReplaying();
            return;
        }
        if (failures.State.IsFailed(partition))
        {
            logger.PartitionToCatchUpIsFailing(partition);
            return;
        }
        if (!lastHandledEventSequenceNumber.IsActualValue)
        {
            logger.LastHandledEventIsNotActualValue();
            return;
        }
        var needCatchupResult = await NeedsCatchup(partition, lastHandledEventSequenceNumber);
        await needCatchupResult.Match(
            needCatchup => needCatchup
                ? StartCatchupJob(partition, lastHandledEventSequenceNumber)
                : Task.CompletedTask,
            error =>
            {
                switch (error)
                {
                    case GetSequenceNumberError.NotFound:
                        logger.LastHandledEventForPartitionUnavailable(partition);
                        return Task.CompletedTask;
                    default:
                        return PartitionFailed(partition, lastHandledEventSequenceNumber.Next(), ["Event Sequence storage error caused partition to try recover"], string.Empty);
                }
            });
    }

    async Task StartCatchupJob(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        var nextEventSequenceNumber = lastHandledEventSequenceNumber.Next();
        logger.StartingCatchUpForPartition(partition, nextEventSequenceNumber);
        State.CatchingUpPartitions.Add(partition);
        await _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(new(_observerKey, Definition.Type, partition, nextEventSequenceNumber, Definition.EventTypes));
        await WriteStateAsync();
    }

    async Task<Result<bool, GetSequenceNumberError>> NeedsCatchup(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        var nextSequenceNumber = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(lastHandledEventSequenceNumber.Next(), _subscription.EventTypes, partition);
        return nextSequenceNumber.Match<Result<bool, GetSequenceNumberError>>(
            number => number.IsActualValue,
            error => error);
    }
}
