// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Runs one attempt to start or resume the job owning an observer's work, as driven by
/// <see cref="JobsManagerExtensions.StartOrResumeObserverJobReportingDrops{TJob, TRequest}"/>.
/// </summary>
/// <typeparam name="TJob">The type of the job.</typeparam>
/// <typeparam name="TRequest">The type of the observer request.</typeparam>
internal sealed class ObserverJobStarter<TJob, TRequest>
    where TJob : IJob<TRequest>
    where TRequest : class, IObserverJobRequest
{
    /// <summary>
    /// Gets the jobs manager.
    /// </summary>
    internal required IJobsManager JobsManager { get; init; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    internal required ILogger Logger { get; init; }

    /// <summary>
    /// Gets the callback creating the request, called after the existing jobs have been looked up.
    /// </summary>
    internal required Func<TRequest> CreateRequest { get; init; }

    /// <summary>
    /// Gets the predicate selecting which existing requests belong to this attempt.
    /// </summary>
    internal required Func<TRequest, bool> RequestPredicate { get; init; }

    /// <summary>
    /// Gets the callback for when there already is a running job.
    /// </summary>
    internal required Func<Task> OnAlreadyRunningJob { get; init; }

    /// <summary>
    /// Gets the callback for when a job is about to be resumed.
    /// </summary>
    internal required Func<Task> OnResume { get; init; }

    /// <summary>
    /// Gets the callback for when a new job is about to be started.
    /// </summary>
    internal required Func<Task> OnStartNew { get; init; }

    /// <summary>
    /// Gets the callback for when a stopped job refused to resume.
    /// </summary>
    internal required Func<Task> OnResumeRefused { get; init; }

    /// <summary>
    /// Gets the optional jobs that have already reported their work as done.
    /// </summary>
    internal ICollection<JobId>? ConcludedJobs { get; init; }

    /// <summary>
    /// Gets the most never-prepared stopped jobs to delete; 0 drops none.
    /// </summary>
    internal int MaxJobsToDrop { get; init; }

    /// <summary>
    /// Gets the optional callback with the number of jobs that were deleted.
    /// </summary>
    internal Action<int>? OnJobsDropped { get; init; }

    /// <summary>
    /// Gets the optional callback stamping the request with its claim before the job is started.
    /// </summary>
    internal Func<TRequest, Task<TRequest>>? StampBeforeStart { get; init; }

    /// <summary>
    /// Gets the optional check for a request carrying an ownership epoch its claim no longer holds.
    /// </summary>
    internal Func<IObserverJobRequest, bool>? IsObsolete { get; init; }

    /// <summary>
    /// Starts or resumes the job.
    /// </summary>
    /// <returns>The <see cref="JobId"/> of the running, resumed, or newly started job; or <see cref="JobId.NotSet"/> if no job could be started.</returns>
    internal async Task<JobId> Run()
    {
        var jobs = await JobsManager.GetOutstandingJobsOf<TJob>();
        var request = CreateRequest();

        // The exclusion is read after the lookup, so it covers jobs that concluded while it was in flight. A listing is no
        // evidence that a concluded job has finished - the jobs manager reports a failed lookup as an empty listing - so
        // nothing is forgotten here. The filter runs to completion before anything else is awaited.
        jobs = SelectJobsOfTheObserver(jobs, request);
        var now = DateTimeOffset.UtcNow;
        jobs = await DeleteAbandonedJobs(jobs, now);

        var alreadyRunningJob = jobs.FirstOrDefault(job => job.IsLive(now));
        if (alreadyRunningJob is not null)
        {
            return await ReportRunningJob(alreadyRunningJob);
        }

        var pausedJobs = jobs.Where(IsResumable).ToList();
        if (MaxJobsToDrop > 0 && !await TryDropJobsThatCanNeverResume(pausedJobs))
        {
            // A job that can never resume blocks the work, or may still do so after a failed delete, so nothing is started until a later lookup lists none. The caller re-arms its own retry.
            return JobId.NotSet;
        }

        var pausedJob = pausedJobs.FirstOrDefault();
        return pausedJob is not null ? await ResumeJob(pausedJob) : await StartNewJob(request);
    }

    ImmutableList<JobState> SelectJobsOfTheObserver(IImmutableList<JobState> jobs, TRequest request) =>
        jobs.Where(job =>
            job.Request is TRequest observerRequest &&
            observerRequest.ObserverKey == request.ObserverKey &&
            RequestPredicate(observerRequest) &&
            !(ConcludedJobs?.Contains(job.Id) ?? false)).ToImmutableList();

    /// <summary>
    /// Deletes jobs stuck in preparation without any step - neither live nor resumable, they would block the work forever.
    /// </summary>
    /// <param name="jobs">The jobs to look through.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The jobs that remain.</returns>
    async Task<IImmutableList<JobState>> DeleteAbandonedJobs(IImmutableList<JobState> jobs, DateTimeOffset now)
    {
        var abandoned = jobs.Where(job => JobLiveness.PreparationAbandoned(job, now)).ToList();
        foreach (var job in abandoned)
        {
            Logger.DeletingAbandonedPreparingJob(job.Id);
            await JobsManager.Delete(job.Id);
        }

        return jobs.RemoveRange(abandoned);
    }

    async Task<JobId> ReportRunningJob(JobState runningJob)
    {
        Logger.FoundRunningJob(runningJob.Id);
        await OnAlreadyRunningJob();
        return runningJob.Id;
    }

    bool IsResumable(JobState job) => job.Status == JobStatus.Stopped && !(IsObsolete?.Invoke((TRequest)job.Request) ?? false);

    async Task<JobId> ResumeJob(JobState pausedJob)
    {
        Logger.FoundStoppedJob(pausedJob.Id);
        await OnResume();

        // Resuming can refuse - the observer is no longer subscribed, the job was never prepared. Reporting
        // the job's id anyway told the caller something is driving catch-up forward when nothing is: the job
        // stays Stopped, never finalizes, and never reports back. The observer is then left preparing catch-up
        // until the watchdog quarantines it, and every retry finds the same stopped job and refuses again.
        if (await JobsManager.Resume(pausedJob.Id))
        {
            return pausedJob.Id;
        }

        Logger.CouldNotResumeJob(pausedJob.Id);
        await OnResumeRefused();
        await RemoveRefusedJobThatNeverPrepared(pausedJob);
        return JobId.NotSet;
    }

    async Task<JobId> StartNewJob(TRequest request)
    {
        Logger.NeedToStartJob();
        await OnStartNew();
        if (StampBeforeStart is not null)
        {
            request = await StampBeforeStart(request);
        }

        var startResult = await JobsManager.Start<TJob, TRequest>(request);
        if (startResult is not null && startResult.TryGetResult(out var jobId))
        {
            return jobId;
        }

        // Starting can legitimately fail - e.g. a node joining a cluster where another node is
        // already handling the same observer job. The observer's watchdog retries later, so
        // failing to start must not take the caller (potentially silo startup) down.
        var error = startResult is not null && startResult.TryGetError(out var startError) ? startError : default;
        Logger.CouldNotStartJob(error);
        return JobId.NotSet;
    }

    /// <summary>
    /// Removes a stopped job whose resume was refused when it was stopped before it prepared any step.
    /// </summary>
    /// <param name="refusedJob">The job that refused to resume.</param>
    /// <remarks>
    /// Such a job has nothing to resume and never will, so every later lookup would find it, refuse it again and leave the
    /// observer retrying catch-up in a tight loop. It carries no work: removing it lets the next catch-up start a job of its
    /// own. A caller that reports drops owns a per-turn budget of them, and this removal takes from that budget like any
    /// other drop, so a turn never deletes more than it allows; the job is then removed by a later turn instead.
    /// </remarks>
    async Task RemoveRefusedJobThatNeverPrepared(JobState refusedJob)
    {
        var budgetAllowsRemoval = OnJobsDropped is null || MaxJobsToDrop > 0;
        if (!JobLiveness.NeverPrepared(refusedJob) || !budgetAllowsRemoval)
        {
            return;
        }

        Logger.RemovingUnresumableJob(refusedJob.Id);
        await JobsManager.Delete(refusedJob.Id);
        OnJobsDropped?.Invoke(1);
    }

    /// <summary>
    /// Deletes up to the allowed number of stopped jobs that were never prepared.
    /// </summary>
    /// <param name="pausedJobs">The stopped jobs.</param>
    /// <returns>True if there was no job that can never resume, so the work can go on; false if any was found, whether or not it could be deleted.</returns>
    /// <remarks>
    /// Deleting returns normally even when the job could not be removed, and a lookup that fails reports an empty listing, so
    /// a deletion is never taken as proof. Once a job has been dropped the caller starts nothing; its next retry looks the
    /// jobs up afresh and goes on only when no such job is listed.
    /// </remarks>
    async Task<bool> TryDropJobsThatCanNeverResume(List<JobState> pausedJobs)
    {
        var unresumableJobs = pausedJobs.Where(job => !job.WasEverPrepared()).ToList();
        if (unresumableJobs.Count == 0)
        {
            return true;
        }

        var dropped = 0;
        foreach (var jobToDrop in unresumableJobs.Take(MaxJobsToDrop))
        {
            if (!await TryDrop(jobToDrop))
            {
                break;
            }

            dropped++;
        }

        OnJobsDropped?.Invoke(dropped);
        return false;
    }

    async Task<bool> TryDrop(JobState jobToDrop)
    {
        try
        {
            Logger.DroppingJobThatCanNeverResume(jobToDrop.Id);
            await JobsManager.Delete(jobToDrop.Id);
            return true;
        }
        catch (Exception ex)
        {
            Logger.CouldNotDropJob(jobToDrop.Id, ex);
            return false;
        }
    }
}
