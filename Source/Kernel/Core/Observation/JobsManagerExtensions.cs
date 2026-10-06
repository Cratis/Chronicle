// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;
namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for <see cref="IJobsManager"/>.
/// </summary>
public static partial class JobsManagerExtensions
{
    static readonly JobStatus[] _unfinishedStatuses =
    [
        JobStatus.None,
        JobStatus.PreparingJob,
        JobStatus.PreparingSteps,
        JobStatus.StartingSteps,
        JobStatus.Running,
        JobStatus.Stopped
    ];

    /// <summary>
    /// Gets the jobs that have not finished - the only ones an observer can pause, resume, stop or wait on.
    /// </summary>
    /// <param name="jobsManager">The jobs manager.</param>
    /// <returns>The jobs that are preparing, running or stopped.</returns>
    /// <remarks>
    /// Finished jobs are retained - those completed with failures for days - and a retry job finishes with failures
    /// every time it runs while the client is away, so they far outnumber the live ones. Every observer looks its
    /// jobs up on every subscribe and unsubscribe. Loading all jobs there deserialized thousands of finished ones
    /// per call, and a fleet of reconnecting clients turned that into hundreds of megabytes of garbage a second:
    /// the garbage collector paused the silo for a quarter of its time, observer turns outlived the clients' 30
    /// second timeout, and every retry added another round. The status filter is served by an index.
    /// </remarks>
    public static Task<IImmutableList<JobState>> GetUnfinishedJobs(this IJobsManager jobsManager) =>
        jobsManager.GetJobs(new JobQuery { Statuses = _unfinishedStatuses, Take = 0 });

    /// <summary>
    /// Starts or resumes an observer job.
    /// </summary>
    /// <param name="jobsManager">The jobs manager.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="request">The observer request.</param>
    /// <param name="requestPredicate">The optional predicate.</param>
    /// <param name="onAlreadyRunningJob">The optional callback when there already is a running job.</param>
    /// <param name="onResume">The optional callback when a job needs to be resumed.</param>
    /// <param name="onStartNew">The optional callback when a new job needs to be started.</param>
    /// <param name="onResumeRefused">The optional callback when a stopped job was found but refused to resume, so nothing owns the work.</param>
    /// <param name="concludedJobs">The optional jobs that have already reported their work as done and therefore can not own it, even while still finalizing. Jobs a lookup listing other jobs no longer lists as unfinished are removed from it.</param>
    /// <typeparam name="TJob">The type of the job.</typeparam>
    /// <typeparam name="TRequest">The type of the observer request.</typeparam>
    /// <returns>The <see cref="JobId"/> of the running, resumed, or newly started job; or <see cref="JobId.NotSet"/> if no job could be started.</returns>
    public static Task<JobId> StartOrResumeObserverJobFor<TJob, TRequest>(
        this IJobsManager jobsManager,
        ILogger logger,
        TRequest request,
        Func<TRequest, bool>? requestPredicate = null,
        Func<Task>? onAlreadyRunningJob = null,
        Func<Task>? onResume = null,
        Func<Task>? onStartNew = null,
        Func<Task>? onResumeRefused = null,
        ICollection<JobId>? concludedJobs = null)
        where TJob : IJob<TRequest>
        where TRequest : class, IObserverJobRequest =>
        jobsManager.StartOrResumeObserverJobFor<TJob, TRequest>(
            logger,
            () => request,
            requestPredicate,
            onAlreadyRunningJob,
            onResume,
            onStartNew,
            onResumeRefused,
            concludedJobs);

    /// <summary>
    /// Starts or resumes an observer job, creating the request only once the existing jobs have been looked up.
    /// </summary>
    /// <param name="jobsManager">The jobs manager.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="createRequest">Creates the observer request. Called after the lookup, so it reflects any state that changed while the lookup was in flight.</param>
    /// <param name="requestPredicate">The optional predicate.</param>
    /// <param name="onAlreadyRunningJob">The optional callback when there already is a running job.</param>
    /// <param name="onResume">The optional callback when a job needs to be resumed.</param>
    /// <param name="onStartNew">The optional callback when a new job needs to be started.</param>
    /// <param name="onResumeRefused">The optional callback when a stopped job was found but refused to resume, so nothing owns the work.</param>
    /// <param name="concludedJobs">The optional jobs that have already reported their work as done and therefore can not own it, even while still finalizing. Jobs a lookup listing other jobs no longer lists as unfinished are removed from it.</param>
    /// <typeparam name="TJob">The type of the job.</typeparam>
    /// <typeparam name="TRequest">The type of the observer request.</typeparam>
    /// <returns>The <see cref="JobId"/> of the running, resumed, or newly started job; or <see cref="JobId.NotSet"/> if no job could be started.</returns>
    /// <remarks>
    /// A job excluded because it concluded while the lookup was in flight has moved the observer on. A request built
    /// before the lookup still carries the position from before that job's work, and a replacement started from it
    /// delivers that work a second time.
    /// </remarks>
    public static async Task<JobId> StartOrResumeObserverJobFor<TJob, TRequest>(
        this IJobsManager jobsManager,
        ILogger logger,
        Func<TRequest> createRequest,
        Func<TRequest, bool>? requestPredicate = null,
        Func<Task>? onAlreadyRunningJob = null,
        Func<Task>? onResume = null,
        Func<Task>? onStartNew = null,
        Func<Task>? onResumeRefused = null,
        ICollection<JobId>? concludedJobs = null)
        where TJob : IJob<TRequest>
        where TRequest : class, IObserverJobRequest
    {
        requestPredicate ??= _ => true;
        onAlreadyRunningJob ??= () => Task.CompletedTask;
        onResume ??= () => Task.CompletedTask;
        onStartNew ??= () => Task.CompletedTask;
        onResumeRefused ??= () => Task.CompletedTask;
        var rememberedBeforeLookup = concludedJobs?.ToArray() ?? [];
        var jobs = await jobsManager.GetUnfinishedJobs();
        var request = createRequest();

        // A concluded job is only dangerous while it is still listed as unfinished - that is what makes it look like an
        // owner. Once a lookup no longer lists it, it has finished for good and can be forgotten, which is what keeps the
        // remembered set bounded. Forgetting by anything else, such as how many jobs concluded after it, readmits a job
        // whose finalization is slow or failed and strands the observer on a job that will never report back again.
        // Only jobs remembered before the lookup started are pruned; one concluding while it was in flight is judged by
        // the next lookup. The exclusion itself is read after the lookup, so it covers jobs that concluded meanwhile.
        // The jobs manager reports a failed lookup as an empty listing, so an empty listing proves nothing has finished;
        // pruning on it would readmit every concluded job still running. Forgetting waits for a listing of other jobs.
        if (concludedJobs is not null && jobs.Count > 0)
        {
            var listed = jobs.Select(job => job.Id).ToHashSet();
            foreach (var finishedJob in rememberedBeforeLookup.Where(id => !listed.Contains(id)))
            {
                concludedJobs.Remove(finishedJob);
            }
        }

        var concluded = concludedJobs?.ToHashSet() ?? [];
        jobs = jobs.Where(job =>
            job.Request is TRequest observerRequest &&
            observerRequest.ObserverKey == request.ObserverKey &&
            requestPredicate(observerRequest) &&
            !concluded.Contains(job.Id)).ToImmutableList();
        var alreadyRunningJob = jobs.FirstOrDefault(job => job.IsPreparingOrRunning);
        if (alreadyRunningJob is not null)
        {
            logger.FoundRunningJob(alreadyRunningJob.Id);
            await onAlreadyRunningJob.Invoke();
            return alreadyRunningJob.Id;
        }

        var pausedJobs = jobs.Where(job => job.Status == JobStatus.Stopped).ToList();
        var pausedJob = pausedJobs.FirstOrDefault();
        if (pausedJob is not null)
        {
            logger.FoundStoppedJob(pausedJob.Id);
            await onResume.Invoke();

            // Resuming can refuse - the observer is no longer subscribed, the job was never prepared. Reporting
            // the job's id anyway told the caller something is driving catch-up forward when nothing is: the job
            // stays Stopped, never finalizes, and never reports back. The observer is then left preparing catch-up
            // until the watchdog quarantines it, and every retry finds the same stopped job and refuses again.
            if (!await jobsManager.Resume(pausedJob.Id))
            {
                logger.CouldNotResumeJob(pausedJob.Id);
                await onResumeRefused.Invoke();
                return JobId.NotSet;
            }

            return pausedJob.Id;
        }

        logger.NeedToStartJob();
        await onStartNew.Invoke();
        var startResult = await jobsManager.Start<TJob, TRequest>(request);
        if (startResult is not null && startResult.TryGetResult(out var jobId))
        {
            return jobId;
        }

        // Starting can legitimately fail - e.g. a node joining a cluster where another node is
        // already handling the same observer job. The observer's watchdog retries later, so
        // failing to start must not take the caller (potentially silo startup) down.
        var error = startResult is not null && startResult.TryGetError(out var startError) ? startError : default;
        logger.CouldNotStartJob(error);
        return JobId.NotSet;
    }
    [LoggerMessage(LogLevel.Debug, "Found already running job {JobId}")]
    static partial void FoundRunningJob(this ILogger logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Found stopped job {JobId}. Resuming it")]
    static partial void FoundStoppedJob(this ILogger logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Need to start new job")]
    static partial void NeedToStartJob(this ILogger logger);

    [LoggerMessage(LogLevel.Warning, "Could not start job: {Error}")]
    static partial void CouldNotStartJob(this ILogger logger, StartJobError error);

    [LoggerMessage(LogLevel.Warning, "Could not resume stopped job {JobId} - nothing will drive it forward")]
    static partial void CouldNotResumeJob(this ILogger logger, JobId jobId);
}
