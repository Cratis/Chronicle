// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    readonly StalledObserverJobs _stalledJobs = new();

    /// <summary>
    /// Gets or sets the clock used to observe stalled job progress.
    /// </summary>
    internal TimeProvider StalledJobsTimeProvider { get; set; } = timeProvider ?? TimeProvider.System;

    bool CanNudgeStalledJobs => !IsQuarantined && _subscription.IsSubscribed && DependsOnObserverJobs;

    bool DependsOnObserverJobs => State.IsReplaying || State.CatchingUpPartitions.Count > 0 || Failures.HasFailedPartitions || _isPreparingCatchup;

    /// <summary>
    /// Resumes this observer's running jobs when step progress has stopped moving. Jobs are listed at most once per <see cref="JobLiveness.ResumeStalledAfter"/>, unless a resume is waiting to be retried.
    /// </summary>
    /// <returns>A task that completes after scheduling the bounded resume attempts.</returns>
    internal async Task NudgeStalledJobs()
    {
        try
        {
            if (!CanNudgeStalledJobs)
            {
                return;
            }

            var now = StalledJobsTimeProvider.GetUtcNow();
            if (!_stalledJobs.ShouldList(now))
            {
                return;
            }

            _stalledJobs.MarkListed(now);
            var jobs = await _jobsManager.GetUnfinishedJobs();
            if (IsQuarantined)
            {
                return;
            }

            now = StalledJobsTimeProvider.GetUtcNow();
            var owned = jobs.Where(_ => IsResumableObserverJob(_, now));
            foreach (var id in _stalledJobs.Find(owned, now))
            {
                logger.ResumingStalledObserverJob(id);

                // The replay resume hook calls back into this observer (GetState/Replay), so awaiting it here would
                // deadlock the non-reentrant grain. The continuation runs on the activation scheduler.
                _ = ResumeStalledJob(id);
            }
        }
        catch (Exception exception)
        {
            logger.FindingStalledObserverJobsFailed(exception);
        }
    }

    bool IsResumableObserverJob(JobState job, DateTimeOffset now) =>
        job.Request is IObserverJobRequest request && request.ObserverKey == _observerKey &&
        !_concludedCatchUpJobs.Contains(job.Id) &&
        !JobLiveness.HasConcludedAllStepsLongAgo(job, now) &&
        (job.Request is not ReplayObserverRequest || State.RunningState == ObserverRunningState.Replaying);

    async Task ResumeStalledJob(JobId id)
    {
        try
        {
            var result = await _jobsManager.ResumeIfRunning(id);
            if (result.TryGetResult(out var success))
            {
                if (success == ResumeJobSuccess.JobIsNotRunning)
                {
                    logger.StalledObserverJobNoLongerRunning(id);
                }

                return;
            }

            if (result.TryGetError(out var error) && error.Value is CannotResumeJobError.Unknown)
            {
                logger.ResumingStalledObserverJobWillRetry(id);
                _stalledJobs.RetryOnNextTick(id);
                return;
            }

            logger.StalledObserverJobNotResumed(id, error?.Value.ToString() ?? "unknown");
        }
        catch (Exception exception)
        {
            logger.ResumingStalledObserverJobFailed(id, exception);
            _stalledJobs.RetryOnNextTick(id);
        }
    }
}
