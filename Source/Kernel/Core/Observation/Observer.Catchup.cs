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

        await AcquireCatchUpJobOwnership(AcquireCatchUpJob);
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
        _catchUpOwnershipEpoch++;
        try
        {
            await HandOverCaughtUpJob(jobId, lastHandledEventSequenceNumber);
        }
        finally
        {
            _catchUpHandoversInFlight--;
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

    /// <inheritdoc/>
    /// <remarks>
    /// The steps of an observer-wide catch-up run independently, and the job reports back only the furthest any of
    /// them got. The partition stays held back until then - live delivery drops its events, so nothing a step reads is
    /// ever delivered a second time - and an event appended after its step read its last one is therefore delivered by
    /// nobody. Finding such an event here keeps the step reading. Otherwise how far the step read is remembered, so an
    /// event appended later still is read before the job's report moves the observer's position past it.
    /// </remarks>
    public async Task<bool> ConcludePartitionCatchUp(Key partition, EventSequenceNumber nextEventSequenceNumber, IEnumerable<EventType> eventTypes)
    {
        // Nothing holds back a partition that is not catching up - routing may already have released it - so reading on
        // would only deliver what live delivery already does.
        if (IsRetired || _removed || !State.CatchingUpPartitions.Contains(partition)) return true;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);

        // Remembered before looking, so even a step that gives up reading on has said how far it got.
        var eventTypesRead = eventTypes.ToArray();
        _catchUpStepsReadUpTo[partition] = (nextEventSequenceNumber, eventTypesRead);

        var unreadEvent = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(nextEventSequenceNumber, eventTypesRead, partition);
        if (unreadEvent.TryGetResult(out var sequenceNumber) && sequenceNumber.IsActualValue)
        {
            logger.PartitionReceivedEventsWhileCatchingUp(partition, sequenceNumber);
            return false;
        }

        // A failed lookup cannot tell whether anything was missed, so the partition is failed from where the step got
        // to and recovered from there, rather than handed back over events nobody may have read.
        if (unreadEvent.TryGetError(out var error) && error != GetSequenceNumberError.NotFound)
        {
            _catchUpStepsReadUpTo.Remove(partition);
            State.CatchingUpPartitions.Remove(partition);
            await PartitionFailed(partition, nextEventSequenceNumber, ["Event Sequence storage error while concluding the partition's catch-up"], string.Empty);
            return true;
        }

        logger.ConcludedPartitionCatchUp(partition);
        return true;
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

        if (await ContinuedCatchUpForPartitionsLeftBehind(lastHandledEventSequenceNumber)) return;

        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        await WriteStateAsync();

        _isPreparingCatchup = false;
        _catchupRecoveryAttempts = 0;

        if (IsRetired || _removed) return;
        await TransitionTo<Routing>();
    }

    /// <summary>
    /// Starts a catch-up for the partitions an observer-wide catch-up left behind, before the position moves past them.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The furthest any step of the reporting job got.</param>
    /// <returns>True if a catch-up now reads the partitions left behind and the observer stays where it is until it reports back; false if the observer can move on.</returns>
    /// <remarks>
    /// Every partition the job caught up is still held back, so an event its step did not read was not delivered live
    /// either. One appended at or below where the position is about to move is left behind for good: routing only looks
    /// for events past the position. The partitions it was left behind for stay held back and are read from where their
    /// own step got to, up to that point and no further - what lies past it is routing's, as for every other partition.
    /// The observer moves on once that catch-up reports back, and checks again then.
    /// <para>
    /// Live delivery of partitions that are not held back interleaves with this check and can move the position further
    /// while it looks, so it looks again until the position it checked up to is where the position is about to move.
    /// </para>
    /// </remarks>
    async Task<bool> ContinuedCatchUpForPartitionsLeftBehind(EventSequenceNumber lastHandledEventSequenceNumber)
    {
        if (_catchUpStepsReadUpTo.Count == 0 || !lastHandledEventSequenceNumber.IsActualValue || IsRetired || _removed)
        {
            _catchUpStepsReadUpTo.Clear();
            return false;
        }

        EventSequenceNumber? checkedUpTo = null;
        while (true)
        {
            var movingPast = GetPositionMovingPast(lastHandledEventSequenceNumber);
            if (checkedUpTo == movingPast) break;

            var partitionsLeftBehind = await FindPartitionsLeftBehind(movingPast);
            checkedUpTo = movingPast;
            if (partitionsLeftBehind.Count > 0 && await StartedCatchUpForPartitionsLeftBehind(partitionsLeftBehind, movingPast))
            {
                return true;
            }
        }

        _catchUpStepsReadUpTo.Clear();
        return false;
    }

    /// <summary>
    /// Gets the last event sequence number the position moves past once the reporting job's work is accepted.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The furthest any step of the reporting job got.</param>
    /// <returns>The <see cref="EventSequenceNumber"/> the position moves past.</returns>
    EventSequenceNumber GetPositionMovingPast(EventSequenceNumber lastHandledEventSequenceNumber) =>
        State.NextEventSequenceNumber.IsActualValue && State.NextEventSequenceNumber > lastHandledEventSequenceNumber.Next()
            ? State.NextEventSequenceNumber - 1
            : lastHandledEventSequenceNumber;

    /// <summary>
    /// Finds the partitions still held back with an event their catch-up did not read, at or below a given point.
    /// </summary>
    /// <param name="movingPast">The last <see cref="EventSequenceNumber"/> the position is about to move past.</param>
    /// <returns>The partitions left behind, each with where reading it has to resume.</returns>
    /// <remarks>
    /// A partition found to have nothing unread up to that point has been read that far, as nothing delivers a held
    /// partition's events but its catch-up, so looking again only has to look past it.
    /// </remarks>
    async Task<List<CatchUpObserverPartitionRange>> FindPartitionsLeftBehind(EventSequenceNumber movingPast)
    {
        var partitionsLeftBehind = new List<CatchUpObserverPartitionRange>();
        foreach (var (partition, (nextToRead, eventTypes)) in _catchUpStepsReadUpTo.ToArray())
        {
            if (!State.CatchingUpPartitions.Contains(partition) || Failures.IsFailed(partition) || nextToRead > movingPast) continue;

            var unreadEvent = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(nextToRead, eventTypes, partition);
            if (unreadEvent.TryGetResult(out var sequenceNumber) && sequenceNumber.IsActualValue && sequenceNumber <= movingPast)
            {
                logger.PartitionLeftBehindByCatchUp(partition, sequenceNumber);
                partitionsLeftBehind.Add(new(partition, nextToRead));
            }
            else if (unreadEvent.TryGetError(out var error) && error != GetSequenceNumberError.NotFound)
            {
                // A failed lookup cannot tell whether anything was left behind, so the partition is recovered from
                // where its catch-up got to rather than skipped by the position moving on.
                _catchUpStepsReadUpTo.Remove(partition);
                State.CatchingUpPartitions.Remove(partition);
                await PartitionFailed(partition, nextToRead, ["Event Sequence storage error while looking for events left behind by the partition's catch-up"], string.Empty);
            }
            else if (_catchUpStepsReadUpTo.ContainsKey(partition))
            {
                _catchUpStepsReadUpTo[partition] = (movingPast.Next(), eventTypes);
            }
        }

        return partitionsLeftBehind;
    }

    /// <summary>
    /// Starts the catch-up that reads the partitions left behind, up to the point the position is about to move past.
    /// </summary>
    /// <param name="partitionsLeftBehind">The partitions left behind, each with where reading it has to resume.</param>
    /// <param name="movingPast">The last <see cref="EventSequenceNumber"/> the position is about to move past.</param>
    /// <returns>True if the catch-up started; false if the partitions were failed instead.</returns>
    async Task<bool> StartedCatchUpForPartitionsLeftBehind(List<CatchUpObserverPartitionRange> partitionsLeftBehind, EventSequenceNumber movingPast)
    {
        var jobId = await AcquireCatchUpJobOwnership(async () =>
        {
            var subscription = await GetSubscription();
            var from = partitionsLeftBehind.Select(_ => _.FromEventSequenceNumber).Aggregate((lowest, next) => next < lowest ? next : lowest);
            var request = new CatchUpObserverRequest(_observerKey, Definition.Type, from, subscription.EventTypes)
            {
                PartitionsLeftBehind = partitionsLeftBehind,
                ToEventSequenceNumber = movingPast
            };

            try
            {
                var startResult = await _jobsManager.Start<ICatchUpObserver, CatchUpObserverRequest>(request);
                return startResult is not null && startResult.TryGetResult(out var startedJobId) ? startedJobId : JobId.NotSet;
            }
            catch (Exception ex)
            {
                logger.FailedStartingCatchUpForPartitionsLeftBehind(ex);
                return JobId.NotSet;
            }
        });

        if (jobId != JobId.NotSet)
        {
            logger.CatchingUpPartitionsLeftBehind(partitionsLeftBehind.Count, movingPast);

            // Once that catch-up has reported back, these have been read up to the point it reads to.
            foreach (var partitionLeftBehind in partitionsLeftBehind)
            {
                if (_catchUpStepsReadUpTo.TryGetValue(partitionLeftBehind.Partition, out var readUpTo))
                {
                    _catchUpStepsReadUpTo[partitionLeftBehind.Partition] = (movingPast.Next(), readUpTo.EventTypes);
                }
            }

            return true;
        }

        // Nothing is going to read them, so they are failed from where their own catch-up got to and recovered from
        // there, rather than skipped by the position moving on.
        foreach (var partitionLeftBehind in partitionsLeftBehind)
        {
            _catchUpStepsReadUpTo.Remove(partitionLeftBehind.Partition);
            State.CatchingUpPartitions.Remove(partitionLeftBehind.Partition);
            await PartitionFailed(partitionLeftBehind.Partition, partitionLeftBehind.FromEventSequenceNumber, ["Could not start catching up events left behind by the partition's catch-up"], string.Empty);
        }

        return false;
    }

    /// <summary>
    /// Acquires a catch-up job as the one catch-up acquisition in flight, so a catch-up interleaving with it adopts it.
    /// </summary>
    /// <param name="acquire">Acquires the job.</param>
    /// <returns>The <see cref="JobId"/> of the job acquired, or <see cref="JobId.NotSet"/> if none was.</returns>
    async Task<JobId> AcquireCatchUpJobOwnership(Func<Task<JobId>> acquire)
    {
        // Set before anything is awaited, so a catch-up interleaving with this one finds it and adopts its outcome.
        var acquisition = new TaskCompletionSource<JobId>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingCatchUpAcquisition = acquisition.Task;
        _catchUpOwnershipEpoch++;
        var jobId = JobId.NotSet;
        try
        {
            jobId = await acquire();
            return jobId;
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
        if (Failures.IsFailed(partition))
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

        // The partition's own job reads it from here on, so no observer-wide catch-up reads it again from where it got to.
        _catchUpStepsReadUpTo.Remove(partition);
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
