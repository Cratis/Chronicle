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
    /// Resumes this observer's running jobs when step progress has stopped moving.
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

            var jobs = await _jobsManager.GetUnfinishedJobs();
            if (IsQuarantined)
            {
                return;
            }

            var owned = jobs.Where(IsResumableObserverJob);
            foreach (var id in _stalledJobs.Find(owned, StalledJobsTimeProvider.GetUtcNow()))
            {
                logger.ResumingStalledObserverJob(id);
                _ = ResumeStalledJob(id);
            }
        }
        catch (Exception exception)
        {
            logger.FindingStalledObserverJobsFailed(exception);
        }
    }

    bool IsResumableObserverJob(JobState job) =>
        job.Request is IObserverJobRequest request && request.ObserverKey == _observerKey &&
        !_concludedCatchUpJobs.Contains(job.Id) &&
        (job.Request is not ReplayObserverRequest || State.RunningState == ObserverRunningState.Replaying);

    async Task ResumeStalledJob(JobId id)
    {
        try
        {
            if (!await _jobsManager.Resume(id))
            {
                logger.StalledObserverJobNotResumed(id);
            }
        }
        catch (Exception exception)
        {
            logger.ResumingStalledObserverJobFailed(id, exception);
        }
    }
}
