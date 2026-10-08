// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public async Task CatchUp()
    {
        if (IsRetired || _removed) return;
        _isPreparingCatchup = true;
        using var scope = logger.BeginObserverScope(State.Identifier, _observerKey);

        if (await AdoptedPendingCatchUpJob()) return;
        if (IsRetired || _removed) return;

        if (State.RunningState == ObserverRunningState.Replaying)
        {
            logger.SkippingCatchUpBecauseObserverIsReplaying();
            _isPreparingCatchup = false;
            return;
        }

        // Set before anything is awaited, so a catch-up interleaving with this one finds it and adopts its outcome.
        var acquisition = new TaskCompletionSource<JobId>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingCatchUpAcquisition = acquisition.Task;
        _catchUpOwnershipEpoch++;
        var jobId = JobId.NotSet;
        try
        {
            jobId = await AcquireCatchUpJob();
        }
        finally
        {
            if (_pendingCatchUpAcquisition == acquisition.Task)
            {
                _pendingCatchUpAcquisition = null;
            }

            // A failed acquisition hands waiters no job, so they acquire one themselves rather than inherit the failure.
            acquisition.SetResult(jobId);
        }
    }

    /// <inheritdoc/>
    public async Task RegisterCatchingUpPartitions(IEnumerable<Key> partitions)
    {
        if (IsRetired || _removed) return;
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
    public async Task CaughtUp(JobId jobId, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        if (IsRetired || _removed) return;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        // From the moment the reporting job stops counting as an owner until routing has asked for its successor, the
        // handover itself owns catch-up. The watchdog interleaves with this call and must not rescue in that window.
        _catchUpHandoversInFlight++;
        _catchUpHandoversSettled ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
        _catchUpOwnershipEpoch++;
        try
        {
            await HandOverCaughtUpJob(jobId, lastHandledEventSequenceNumber);
        }
        finally
        {
            if (--_catchUpHandoversInFlight == 0 && _catchUpHandoversSettled is { } settled)
            {
                _catchUpHandoversSettled = null;
                settled.SetResult();
            }
        }
    }

    /// <inheritdoc/>
    public async Task PartitionCaughtUp(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        if (IsRetired || _removed) return;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.PartitionCaughtUp(partition, lastHandledEventSequenceNumber);
        State.CatchingUpPartitions.Remove(partition);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();
        await StartCatchupJobIfNeeded(partition, lastHandledEventSequenceNumber);
    }

    async Task HandOverCaughtUpJob(JobId jobId, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        // The job reports back before it is finalized, so it is still listed as running while routing decides
        // whether the observer is behind. Finding it there must not count as an owner: it has done its work and will
        // never report back again, which would leave an event appended at that boundary without anyone to handle it.
        // Only the job reporting back is excluded - any other catch-up job, such as one started after it was
        // finalized, is still live and still owns its work. Recorded before anything is awaited, so a catch-up
        // interleaving with this call already sees it.
        RememberConcludedCatchUpJob(jobId);

        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        State.CatchingUpPartitions.Clear();
        await WriteStateAsync();

        _isPreparingCatchup = false;
        _catchupRecoveryAttempts = 0;

        if (IsRetired || _removed) return;
        if (!IsQuarantined)
        {
            await TransitionTo<Routing>();
        }
    }

    /// <summary>
    /// Waits for any catch-up job acquisition already in flight and adopts the job it produced.
    /// </summary>
    /// <returns>True if a live job from an acquisition in flight now owns catch-up; false if this catch-up must acquire one.</returns>
    /// <remarks>
    /// A job that has already concluded - one adopted by a lookup that ran before it reported back - or no job at all
    /// does not own anything, so the waiting catch-up goes on to acquire one itself.
    /// </remarks>
    async Task<bool> AdoptedPendingCatchUpJob()
    {
        var pending = _pendingCatchUpAcquisition;
        while (pending is not null)
        {
            var jobId = await pending;
            if (jobId != JobId.NotSet && !_concludedCatchUpJobs.Contains(jobId))
            {
                logger.AdoptingPendingCatchUpJob(jobId);
                return true;
            }

            pending = _pendingCatchUpAcquisition == pending ? null : _pendingCatchUpAcquisition;
        }

        return false;
    }

    async Task<JobId> AcquireCatchUpJob()
    {
        await ForgetFinishedConcludedCatchUpJobs();
        var subscription = await GetSubscription();
        return await _jobsManager.StartOrResumeObserverJobFor<ICatchUpObserver, CatchUpObserverRequest>(
            logger,

            // Created after the lookup: a job concluding while it is in flight is excluded and has moved the position on.
            () => new(_observerKey, Definition.Type, State.NextEventSequenceNumber, subscription.EventTypes),
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
            },

            // A stopped job that refuses to resume is nobody's: it will not run, will not finalize, and will never
            // report back, so nothing is ever going to lower the flag again. Leaving it raised makes Handle drop
            // every live event and makes Observing skip its missed-events check, and the watchdog then quarantines
            // the observer for a strand that was only ever a job nobody took. A job that fails to *start* is a
            // different situation and keeps its existing retry-then-quarantine handling.
            () =>
            {
                logger.NoCatchUpJobTookOwnership();
                _isPreparingCatchup = false;
                return Task.CompletedTask;
            },
            concludedJobs: _concludedCatchUpJobs);
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
        if (IsRetired || _removed) return;
        var nextEventSequenceNumber = lastHandledEventSequenceNumber.Next();
        logger.StartingCatchUpForPartition(partition, nextEventSequenceNumber);
        State.CatchingUpPartitions.Add(partition);
        await _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(new(_observerKey, Definition.Type, partition, nextEventSequenceNumber, Definition.EventTypes));
        await WriteStateAsync();
    }

    /// <summary>
    /// Remembers a catch-up job that has reported its work as done, so no catch-up takes it as an owner.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job that concluded.</param>
    /// <remarks>
    /// Forgotten by <see cref="ForgetFinishedConcludedCatchUpJobs"/> once the job store confirms it has finished.
    /// </remarks>
    void RememberConcludedCatchUpJob(JobId jobId)
    {
        if (jobId == JobId.NotSet) return;
        _concludedCatchUpJobs.Add(jobId);
    }

    /// <summary>
    /// Forgets the remembered concluded catch-up jobs the job store confirms have finished or no longer exist.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A concluded job is only dangerous while it is unfinished - that is what makes it look like an owner. Once it has
    /// finished it can be forgotten, which is what keeps the remembered set bounded on an activation that is kept alive.
    /// Each job is confirmed on its own, because a listing cannot tell a finished job from a failed lookup: the jobs
    /// manager reports that failure as an empty listing, so an idle namespace never proved anything finished and every
    /// catch-up left its job behind. A failed lookup forgets nothing, since readmitting a concluded job that is still
    /// finalizing strands the observer on a job that will never report back again. A job concluding meanwhile is not
    /// in the snapshot and is judged next time.
    /// </remarks>
    async Task ForgetFinishedConcludedCatchUpJobs()
    {
        if (_concludedCatchUpJobs.Count == 0) return;
        var jobStorage = storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Jobs;
        foreach (var jobId in _concludedCatchUpJobs.ToArray())
        {
            var job = await jobStorage.GetJob(jobId);
            var hasFinished =
                (job.TryGetError(out var error) && error == Cratis.Orleans.Storage.Jobs.JobError.NotFound) ||
                (job.TryGetResult(out var state) && state.Status.HasFinished());
            if (hasFinished)
            {
                _concludedCatchUpJobs.Remove(jobId);
            }
        }
    }

    async Task<Result<bool, GetSequenceNumberError>> NeedsCatchup(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        var nextSequenceNumber = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(lastHandledEventSequenceNumber.Next(), _subscription.EventTypes, partition);
        return nextSequenceNumber.Match<Result<bool, GetSequenceNumberError>>(
            number => number.IsActualValue,
            error => error);
    }
}
