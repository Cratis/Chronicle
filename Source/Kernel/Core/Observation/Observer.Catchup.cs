// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    const int MinimumZeroWorkLapsBeforeBackingOff = 2;
    static readonly TimeSpan _catchUpBackoffBase = TimeSpan.FromSeconds(5);
    static readonly TimeSpan _catchUpBackoffCap = TimeSpan.FromMinutes(5);
    static readonly TimeSpan[] _shortCatchUpRetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)];

    int _partitionHoldsVersion;
    int _catchUpConclusionsInFlight;

    /// <summary>
    /// Completes when every concluded catch-up job reporting back has had its position applied; null while none is being applied.
    /// </summary>
    TaskCompletionSource? _catchUpConclusionsApplied;

    bool CatchUpObserverRemoved => IsRetired || _removed;

    bool IsBackingOffCatchUp => _catchUpBackoffUntil != default && DateTimeOffset.UtcNow < _catchUpBackoffUntil;

    /// <inheritdoc/>
    public async Task CatchUp()
    {
        if (CatchUpObserverRemoved) return;
        _isPreparingCatchup = true;
        _walkingEveryPartition = false;
        using var scope = logger.BeginObserverScope(State.Identifier, _observerKey);

        if (await AdoptedPendingCatchUpJob()) return;
        if (CatchUpObserverRemoved) return;

        if (State.RunningState == ObserverRunningState.Replaying)
        {
            logger.SkippingCatchUpBecauseObserverIsReplaying();
            _isPreparingCatchup = false;
            NotePreparationEnded();
            await RepairMissedDeliveries();
            return;
        }

        await ResumePendingCatchUpRepairs();
        using var admission = await AdmitCatchUpOrDefer();
        if (admission is null) return;

        // Set before anything is awaited, so a catch-up interleaving with this one finds it and adopts its outcome.
        var acquisition = new TaskCompletionSource<JobId>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingCatchUpAcquisition = acquisition.Task;
        _catchUpOwnershipEpoch++;
        var jobId = JobId.NotSet;
        try
        {
            var stampedEpoch = default(ulong?);
            await LiveDeliveriesSettled();
            jobId = await AcquireCatchUpJob(epoch => stampedEpoch = epoch);
            ClaimCatchUpJob(jobId, stampedEpoch);
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
        _walkingEveryPartition = _subscription.DeliversPartitionPages;

        // Reached when a brand-new job's steps prepared. Only a job that has partitions to catch up is forward
        // progress; one with none is the zero-work lap and must not reset the consecutive-stranding count the
        // watchdog uses to decide when to give up.
        if (State.CatchingUpPartitions.Count > 0)
        {
            _catchupRecoveryAttempts = 0;
        }
    }

    /// <inheritdoc/>
    public Task CaughtUp(JobId jobId, EventSequenceNumber lastHandledEventSequenceNumber) =>
        CaughtUp(jobId, lastHandledEventSequenceNumber, EventSequenceNumber.Unavailable);

    /// <inheritdoc/>
    public Task CaughtUp(EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber) =>
        CaughtUp(JobId.NotSet, lastHandledEventSequenceNumber, lastScannedEventSequenceNumber);

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
    /// <para>
    /// Events the observer's filters exclude are read but never handled, so the last handled event cannot say how
    /// far catch-up got. The next event sequence number moves past the last event read as well; otherwise the
    /// excluded events after the last handled one look unhandled to routing, which would start another catch-up
    /// that reads them again and concludes the same way, without end.
    /// </para>
    /// </remarks>
    public async Task CaughtUp(JobId jobId, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber)
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
            await HandOverCaughtUpJob(jobId, lastHandledEventSequenceNumber, lastScannedEventSequenceNumber);
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
    public Task PartitionCaughtUp(Key partition, EventSequenceNumber lastHandledEventSequenceNumber) =>
        PartitionCaughtUp(partition, lastHandledEventSequenceNumber, EventSequenceNumber.Unavailable);

    /// <inheritdoc/>
    /// <remarks>
    /// Events the observer's filters exclude are read but never handled. Whether the partition needs another
    /// catch-up is therefore decided from the furthest event read, so trailing excluded events do not start a
    /// catch-up that reads them again, while only the handled events count as handled.
    /// </remarks>
    public Task PartitionCaughtUp(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber) =>
        PartitionCaughtUp(partition, lastHandledEventSequenceNumber, lastScannedEventSequenceNumber, null);

    /// <inheritdoc/>
    public async Task PartitionCaughtUp(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber, ulong? ownershipEpoch)
    {
        if (CatchUpObserverRemoved || IgnoresStaleRelease(ClaimHolderKind.PartitionCatchUp, partition, ownershipEpoch)) return;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        Skips.Completing(partition, FurthestOf(lastHandledEventSequenceNumber, lastScannedEventSequenceNumber));
        logger.PartitionCaughtUp(partition, lastHandledEventSequenceNumber);
        ShadowHandOverPartition(partition, ClaimHolderKind.PartitionCatchUp);
        State.CatchingUpPartitions.Remove(partition);
        _partitionHoldsVersion++;
        HandleNewLastHandledEventForPartition(partition, lastHandledEventSequenceNumber);
        CompleteHeldBackDelivery(partition, FurthestOf(lastHandledEventSequenceNumber, lastScannedEventSequenceNumber));
        if (PendingCatchUpRepairs.Any(_ => _.Partition == partition))
        {
            CompletePendingCatchUpRepair(partition);
            await ConfirmCatchUpAcceptance();
            ScheduleCatchUpContinuation(madeProgress: true);
            return;
        }

        await WriteStateAsync();
        await StartCatchupJobIfNeeded(partition, FurthestOf(lastHandledEventSequenceNumber, lastScannedEventSequenceNumber));
        EvaluateSuspectedSkips();
    }

    static EventSequenceNumber FurthestOf(EventSequenceNumber lastHandled, EventSequenceNumber lastScanned)
    {
        if (!lastScanned.IsActualValue)
        {
            return lastHandled;
        }

        return !lastHandled.IsActualValue || lastScanned > lastHandled ? lastScanned : lastHandled;
    }

    async Task<ISet<Key>> PartitionHoldsReleasableAfterCatchUp()
    {
        if (State.CatchingUpPartitions.Count == 0) return new HashSet<Key>();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var partitions = State.CatchingUpPartitions.ToArray();
            var version = CurrentPartitionHoldsVersion();
            try
            {
                var result = await storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).Jobs.GetOutstandingJobs();
                if (!result.TryGetResult(out var jobs)) return new HashSet<Key>();
                if (version != CurrentPartitionHoldsVersion()) continue;

                var owned = jobs.PartitionsHeldBy<CatchUpObserverPartitionRequest>(_observerKey, DateTimeOffset.UtcNow, State);
                return partitions.Where(partition => !owned.Contains(partition)).ToHashSet();
            }
            catch (Exception)
            {
                return new HashSet<Key>();
            }
        }

        return new HashSet<Key>();
    }

    async Task HandOverCaughtUpJob(JobId jobId, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber)
    {
        // The job reports back before it is finalized, so it is still listed as running while routing decides
        // whether the observer is behind. Finding it there must not count as an owner: it has done its work and will
        // never report back again, which would leave an event appended at that boundary without anyone to handle it.
        // Only the job reporting back is excluded - any other catch-up job, such as one started after it was
        // finalized, is still live and still owns its work. Recorded before anything is awaited, so a catch-up
        // interleaving with this call already sees it.
        // Until the job's position is applied, a catch-up that excludes it would start over what it already handled.
        RememberConcludedCatchUpJob(jobId);
        BeginCatchUpConclusion();
        try
        {
            var concluded = await ApplyCatchUpConclusion(jobId, lastHandledEventSequenceNumber, lastScannedEventSequenceNumber);
            if (concluded is not bool madeProgress) return;

            Skips.RoundConcluded(FurthestOf(lastHandledEventSequenceNumber, lastScannedEventSequenceNumber));
            Skips.PreparationEnded();
            QueueUnownedHeldBackDeliveries();
            AcceptPendingCatchUpRepairs();
            Skips.ClearJobReported();
            await ConfirmCatchUpAcceptance();

            _isPreparingCatchup = false;
            ScheduleCatchUpContinuation(madeProgress);
        }
        finally
        {
            // Applying a cursor is not a handover until storage accepted its repair obligations as well.
            EndCatchUpConclusion();
        }
    }

    /// <summary>
    /// Applies the position a concluded catch-up job reached.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> of the job that concluded.</param>
    /// <param name="lastHandledEventSequenceNumber">The last handled event sequence number the concluded job reports.</param>
    /// <param name="lastScannedEventSequenceNumber">The last scanned event sequence number the concluded job reports.</param>
    /// <returns>Whether the lap made progress; null if a newer catch-up owns what the job concluded.</returns>
    async Task<bool?> ApplyCatchUpConclusion(JobId jobId, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber)
    {
        // The observer-wide job only owned the partitions it registered. A partition's own catch-up
        // keeps its hold until it reports PartitionCaughtUp (#5379).
        var releasable = await PartitionHoldsReleasableAfterCatchUp();

        // A job whose claim has passed to a newer catch-up concluded nothing the newer one owns: its holds, cursor,
        // preparation and routing all belong to the newer job, which reports back on its own.
        if (!await ShadowHandOverCaughtUpObserverWide(jobId)) return null;

        var madeProgress = CatchUpMadeProgress(lastHandledEventSequenceNumber, lastScannedEventSequenceNumber);
        HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
        State.CatchingUpPartitions.ExceptWith(releasable);
        if (HasScannedUpToNext(lastScannedEventSequenceNumber))
        {
            State = State with { NextEventSequenceNumber = lastScannedEventSequenceNumber.Next() };
        }

        return madeProgress;
    }

    async Task ContinueAfterCatchUpLap(bool settleInObserving)
    {
        if (IsCatchUpEnded()) return;

        if (settleInObserving)
        {
            await SettleCatchUpInObserving();
        }
        else
        {
            await TransitionTo<Routing>();
        }
    }

    /// <summary>
    /// Gets whether the observer moved past anything it had not already handled or read.
    /// </summary>
    /// <param name="lastHandledEventSequenceNumber">The last handled event sequence number the concluded job reports.</param>
    /// <param name="lastScannedEventSequenceNumber">The last scanned event sequence number the concluded job reports.</param>
    /// <returns>True if the lap made progress.</returns>
    bool CatchUpMadeProgress(EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber) =>
        (lastHandledEventSequenceNumber.IsActualValue &&
            (!State.LastHandledEventSequenceNumber.IsActualValue || lastHandledEventSequenceNumber > State.LastHandledEventSequenceNumber)) ||
        HasScannedUpToNext(lastScannedEventSequenceNumber);

    bool HasScannedUpToNext(EventSequenceNumber lastScannedEventSequenceNumber) =>
        lastScannedEventSequenceNumber.IsActualValue &&
            (!State.NextEventSequenceNumber.IsActualValue || State.NextEventSequenceNumber <= lastScannedEventSequenceNumber);

    /// <summary>
    /// Records the outcome of a concluded catch-up lap.
    /// </summary>
    /// <param name="madeProgress">Whether the lap moved the observer forward.</param>
    /// <returns>True if the observer should settle into observing rather than route again immediately.</returns>
    /// <remarks>
    /// A lap that moved nothing reaches the same verdict when routed again, so it neither resets the consecutive
    /// recovery count nor gets to start the next lap right away: from the second consecutive one the observer settles
    /// into observing and the watchdog retries once an exponentially growing backoff has elapsed. When that lap would
    /// settle while events of the observer's types still exist from where it is, a few short retries come first (#5351):
    /// they hold the observer in observing for the length of the retry delay and are not counted towards the backoff.
    /// </remarks>
    async Task<bool> NoteCatchUpLap(bool madeProgress)
    {
        CancelShortCatchUpRetry();
        if (madeProgress)
        {
            _catchupRecoveryAttempts = 0;
            _zeroWorkCatchUps = 0;
            _shortCatchUpRetries = 0;
            _catchUpBackoffUntil = default;
            return false;
        }

        _metrics?.ZeroWorkCatchUp();
        if (_zeroWorkCatchUps + 1 >= MinimumZeroWorkLapsBeforeBackingOff && await ScheduleShortCatchUpRetry())
        {
            return true;
        }

        _zeroWorkCatchUps++;
        logger.CatchUpProducedNoWork(_zeroWorkCatchUps);
        if (_zeroWorkCatchUps < MinimumZeroWorkLapsBeforeBackingOff)
        {
            return false;
        }

        var exponent = Math.Min(_zeroWorkCatchUps - MinimumZeroWorkLapsBeforeBackingOff, 10);
        var backoff = TimeSpan.FromTicks(Math.Min(_catchUpBackoffBase.Ticks << exponent, _catchUpBackoffCap.Ticks));
        _catchUpBackoffUntil = DateTimeOffset.UtcNow + backoff;
        return true;
    }

    void CancelShortCatchUpRetry()
    {
        _shortCatchUpRetryTimer?.Dispose();
        _shortCatchUpRetryTimer = null;
    }

    /// <summary>
    /// Schedules a short retry of a catch-up that moved nothing, when events of the observer's types exist from where it is.
    /// </summary>
    /// <returns>True if a retry was scheduled; false to use the zero-work backoff.</returns>
    /// <remarks>
    /// The decision is read from the event sequence storage, the same lookup the watchdog uses, with the types the
    /// observer is subscribed with. A failed lookup is not an answer: the zero-work backoff applies as before.
    /// The observer settles in observing for the delay, so the pending retry keeps the backoff mechanics - and the
    /// watchdog - as the fallback if the timer never fires.
    /// </remarks>
    async Task<bool> ScheduleShortCatchUpRetry()
    {
        var next = State.NextEventSequenceNumber;
        var epoch = _catchUpOwnershipEpoch;
        try
        {
            var lookup = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(next, _subscription.EventTypes.ToList());
            if (lookup.TryGetError(out var error) && error != GetSequenceNumberError.NotFound)
            {
                logger.CatchUpEventLookupErrored(next, error);
                return false;
            }

            if (!lookup.Match(number => number.IsActualValue, _ => false))
            {
                _shortCatchUpRetries = 0;
                return false;
            }
        }
        catch (Exception exception)
        {
            logger.CatchUpEventLookupFailed(next, exception);
            return false;
        }

        // CaughtUp interleaves, so catch-up may have changed hands while the lookup was awaited. This lap is obsolete then.
        if (epoch != _catchUpOwnershipEpoch) return false;

        if (_shortCatchUpRetries >= _shortCatchUpRetryDelays.Length)
        {
            logger.StaleCatchUpRetriesExhausted(_shortCatchUpRetries, next);
            return false;
        }

        var delay = _shortCatchUpRetryDelays[_shortCatchUpRetries++];
        logger.RetryingStaleCatchUp(next, _shortCatchUpRetries, delay);
        _catchUpBackoffUntil = DateTimeOffset.UtcNow + delay;
        CancelShortCatchUpRetry();
        _shortCatchUpRetryTimer = this.RegisterGrainTimer(
            cancellationToken => RetryCatchUpAfterDelay(epoch, cancellationToken),
            new GrainTimerCreationOptions { DueTime = delay, Period = Timeout.InfiniteTimeSpan });
        return true;
    }

    async Task RetryCatchUpAfterDelay(int ownershipEpoch, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        CancelShortCatchUpRetry();
        if (!CanRetryCatchUp(ownershipEpoch)) return;

        _catchUpBackoffUntil = default;
        await TransitionTo<Routing>();
    }

    void ScheduleDeferredCatchUpRetry(TimeSpan delay)
    {
        var epoch = _catchUpOwnershipEpoch;
        CancelShortCatchUpRetry();
        _shortCatchUpRetryTimer = this.RegisterGrainTimer(
            cancellationToken => RetryDeferredCatchUp(epoch, cancellationToken),
            new GrainTimerCreationOptions { DueTime = delay, Period = Timeout.InfiniteTimeSpan });
    }

    async Task RetryDeferredCatchUp(int ownershipEpoch, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return;

        CancelShortCatchUpRetry();

        // The observer settles in observing after it was deferred; anywhere else the watchdog is the fallback.
        if (!CanRetryCatchUp(ownershipEpoch) || await GetCurrentState() is not Observing) return;

        _catchUpBackoffUntil = default;
        await TransitionTo<Routing>();
    }

    bool CanRetryCatchUp(int ownershipEpoch) =>
        IsObservingOwnedCatchUp(ownershipEpoch) && State.RunningState != ObserverRunningState.Replaying && _subscription.IsSubscribed;

    bool IsObservingOwnedCatchUp(int ownershipEpoch) =>
        !IsCatchUpEnded() && ownershipEpoch == _catchUpOwnershipEpoch;

    bool IsCatchUpEnded() => IsRetired || _removed || IsQuarantined;

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

    /// <summary>
    /// Records the catch-up job acquired in the observer-wide claim.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> acquired; <see cref="JobId.NotSet"/> if none was.</param>
    /// <param name="stampedEpoch">The epoch a newly started job was stamped with; null if the job was adopted, resumed or runs unchecked.</param>
    /// <remarks>
    /// A job started under a granted claim is attached to it. Any other job is recorded in a claim of its own, as before
    /// claims were enforced, unless a catch-up claim is already held - granting over it would leave a resumed or adopted
    /// job carrying an epoch no longer held - or a replay holds the observer, which catch-up never takes over.
    /// </remarks>
    void ClaimCatchUpJob(JobId jobId, ulong? stampedEpoch)
    {
        if (stampedEpoch is ulong epoch)
        {
            if (jobId != JobId.NotSet) AttachObserverWideJob(epoch, jobId);
            return;
        }

        var holder = _shadowClaims.ObserverWide?.Holder;
        if (holder is not ClaimHolderKind.ObserverCatchUp and not ClaimHolderKind.ObserverReplay)
        {
            ShadowGrantObserverWide(ClaimHolderKind.ObserverCatchUp, jobId, State.NextEventSequenceNumber);
        }
    }

    async Task<JobId> AcquireCatchUpJob(Action<ulong?> onStamped)
    {
        await ForgetFinishedConcludedCatchUpJobs();
        var subscription = await GetSubscription();
        return await _jobsManager.StartOrResumeObserverJobReportingDrops<ICatchUpObserver, CatchUpObserverRequest>(
            logger,

            // Created after the lookup, and moved on before the start to where a job that concluded meanwhile reached.
            () => new(_observerKey, Definition.Type, State.NextEventSequenceNumber, subscription.EventTypes) { ReaderScope = ReaderScope },
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
                NotePreparationEnded();
                return RepairMissedDeliveries();
            },
            _concludedCatchUpJobs,
            0,
            null,
            async request =>
            {
                // A job that concluded during the lookup was excluded before its position was applied (#6145).
                await CatchUpConclusionsApplied();
                request = request with { FromEventSequenceNumber = State.NextEventSequenceNumber };
                var epoch = await GrantObserverWideBeforeStart(ClaimHolderKind.ObserverCatchUp, request.FromEventSequenceNumber);
                onStamped(epoch);
                return request with { OwnershipEpoch = epoch };
            },
            IsObsolete);
    }

    async Task StartCatchupJobIfNeeded(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, bool fromStartWhenNothingRead = false)
    {
        // The completed job's partition stays owned until the decision about a successor is made.
        var ownership = Skips.OwnershipTicket(partition);
        try
        {
            await ProbeAndStartCatchupJob(partition, lastHandledEventSequenceNumber, fromStartWhenNothingRead);
        }
        finally
        {
            Skips.Release(partition, ownership);
        }

        CompleteHeldBackDelivery(partition, lastHandledEventSequenceNumber);
        await EvaluateAndRepairMissedDeliveries();
    }

    async Task ProbeAndStartCatchupJob(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, bool fromStartWhenNothingRead)
    {
        if (!lastHandledEventSequenceNumber.IsActualValue && !fromStartWhenNothingRead)
        {
            logger.LastHandledEventIsNotActualValue();
            return;
        }

        // With no read position the partition has no known position beyond its start, so catch-up begins there.
        var fromSequenceNumber = lastHandledEventSequenceNumber.IsActualValue ? lastHandledEventSequenceNumber.Next() : EventSequenceNumber.First;
        await ProbeAndStartCatchupJobFrom(partition, fromSequenceNumber);
    }

    async Task ProbeAndStartCatchupJobFrom(Key partition, EventSequenceNumber fromSequenceNumber)
    {
        if (!fromSequenceNumber.IsActualValue) return;

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
        Skips.ProbedFrom(partition, fromSequenceNumber);
        var needCatchupResult = await NeedsCatchup(partition, fromSequenceNumber);
        await needCatchupResult.Match(
            needCatchup => needCatchup
                ? StartCatchupJob(partition, fromSequenceNumber)
                : Task.CompletedTask,
            error =>
            {
                switch (error)
                {
                    case GetSequenceNumberError.NotFound:
                        logger.LastHandledEventForPartitionUnavailable(partition);
                        return Task.CompletedTask;
                    default:
                        return PartitionFailed(partition, fromSequenceNumber, ["Event Sequence storage error caused partition to try recover"], string.Empty);
                }
            });
    }

    async Task StartCatchupJob(Key partition, EventSequenceNumber nextEventSequenceNumber)
    {
        if (IsRetired || _removed) return;
        if (IsWithoutClient)
        {
            DeferringPartitionCatchUpWithoutClient(logger, partition);
            return;
        }

        logger.StartingCatchUpForPartition(partition, nextEventSequenceNumber);
        Skips.JobStarted(partition);

        // The hold and claim are persisted before the job starts, so admission finds the claim the job is stamped with.
        using var start = BeginPartitionJobStart();
        var epochs = await GrantPartitionsBeforeStart([partition], ClaimHolderKind.PartitionCatchUp, nextEventSequenceNumber);
        if (PartitionJobStartAbandoned()) return;
        await _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
            new(_observerKey, Definition.Type, partition, nextEventSequenceNumber, Definition.EventTypes) { ReaderScope = ReaderScope, OwnershipEpoch = epochs[partition] });
    }

    bool PartitionJobStartAbandoned() => IsRetired || _removed || !_observerExists;

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

    void BeginCatchUpConclusion()
    {
        _catchUpConclusionsInFlight++;
        _catchUpConclusionsApplied ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    void EndCatchUpConclusion()
    {
        if (--_catchUpConclusionsInFlight == 0 && _catchUpConclusionsApplied is { } applied)
        {
            _catchUpConclusionsApplied = null;
            applied.SetResult();
        }
    }

    /// <summary>
    /// Waits until every concluded catch-up job reporting back has had its position applied.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A concluded job stops counting as an owner before its position is applied, as the handover awaits storage in
    /// between. A catch-up that looked up the jobs in that window found no owner and started a new job from the old
    /// position, delivering everything the concluded job had already handled a second time (#6145). Waiting after
    /// the lookup and before the job is started, then taking the position afresh, closes the window: the handover routes only after it, and a
    /// catch-up it routes into adopts the acquisition that is waiting.
    /// </remarks>
    async Task CatchUpConclusionsApplied()
    {
        while (_catchUpConclusionsApplied is { } applied)
        {
            await applied.Task;
        }
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
                (job.TryGetError(out var error) && error == Cratis.Choreography.Storage.Jobs.JobError.NotFound) ||
                (job.TryGetResult(out var state) && state.Status.HasFinished());
            if (hasFinished)
            {
                _concludedCatchUpJobs.Remove(jobId);
            }
        }
    }

    async Task<Result<bool, GetSequenceNumberError>> NeedsCatchup(Key partition, EventSequenceNumber fromSequenceNumber)
    {
        var nextSequenceNumber = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(fromSequenceNumber, _subscription.EventTypes, partition);
        return nextSequenceNumber.Match<Result<bool, GetSequenceNumberError>>(
            number => number.IsActualValue,
            error => error);
    }
}
