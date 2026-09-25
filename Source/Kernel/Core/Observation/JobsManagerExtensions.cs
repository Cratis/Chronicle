// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;
namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for <see cref="IJobsManager"/>.
/// </summary>
public static partial class JobsManagerExtensions
{
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
    /// <typeparam name="TJob">The type of the job.</typeparam>
    /// <typeparam name="TRequest">The type of the observer request.</typeparam>
    /// <returns>The <see cref="JobId"/> of the running, resumed, or newly started job; or <see cref="JobId.NotSet"/> if no job could be started.</returns>
    public static async Task<JobId> StartOrResumeObserverJobFor<TJob, TRequest>(
        this IJobsManager jobsManager,
        ILogger logger,
        TRequest request,
        Func<TRequest, bool>? requestPredicate = null,
        Func<Task>? onAlreadyRunningJob = null,
        Func<Task>? onResume = null,
        Func<Task>? onStartNew = null,
        Func<Task>? onResumeRefused = null)
        where TJob : IJob<TRequest>
        where TRequest : class, IObserverJobRequest
    {
        requestPredicate ??= _ => true;
        onAlreadyRunningJob ??= () => Task.CompletedTask;
        onResume ??= () => Task.CompletedTask;
        onStartNew ??= () => Task.CompletedTask;
        onResumeRefused ??= () => Task.CompletedTask;

        var jobs = await jobsManager.GetJobsOfType<TJob, TRequest>();
        jobs = jobs.Where(job => job.Request is TRequest observerRequest && observerRequest.ObserverKey == request.ObserverKey && requestPredicate(observerRequest)).ToImmutableList();
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
