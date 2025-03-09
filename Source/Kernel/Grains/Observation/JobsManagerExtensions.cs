// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Microsoft.Extensions.Logging;
namespace Cratis.Chronicle.Grains.Observation;

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
    /// <typeparam name="TJob">The type of the job.</typeparam>
    /// <typeparam name="TRequest">The type of the observer request.</typeparam>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task StartOrResumeObserverJobFor<TJob, TRequest>(
        this IJobsManager jobsManager,
        ILogger logger,
        TRequest request,
        Func<TRequest, bool>? requestPredicate = null,
        Func<Task>? onAlreadyRunningJob = null,
        Func<Task>? onResume = null,
        Func<Task>? onStartNew = null)
        where TJob : IJob<TRequest>
        where TRequest : class, IObserverJobRequest
    {
        requestPredicate ??= _ => true;
        onAlreadyRunningJob ??= () => Task.CompletedTask;
        onResume ??= () => Task.CompletedTask;
        onStartNew ??= () => Task.CompletedTask;

        var jobs = await jobsManager.GetJobsOfType<TJob, TRequest>();
        jobs = jobs.Where(job => job.Request is TRequest observerRequest && observerRequest.ObserverKey == request.ObserverKey && requestPredicate(observerRequest)).ToImmutableList();
        var alreadyRunningJob = jobs.FirstOrDefault(job => job.IsPreparingOrRunning);
        if (alreadyRunningJob is not null)
        {
            logger.FoundRunningJob(alreadyRunningJob.Id);
            await onAlreadyRunningJob.Invoke();
            return;
        }

        var pausedJobs = jobs.Where(job => job.Status == JobStatus.Stopped).ToList();
        var pausedJob = pausedJobs.FirstOrDefault();
        if (pausedJob is not null)
        {
            logger.FoundStoppedJob(pausedJob.Id);
            await onResume.Invoke();
            await jobsManager.Resume(pausedJob.Id);
        }
        else
        {
            logger.NeedToStartJob();
            await onStartNew.Invoke();
            await jobsManager.Start<TJob, TRequest>(request);
        }
    }
    [LoggerMessage(LogLevel.Debug, "Found already running job {JobId}")]
    static partial void FoundRunningJob(this ILogger logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Found stopped job {JobId}. Resuming it")]
    static partial void FoundStoppedJob(this ILogger logger, JobId jobId);

    [LoggerMessage(LogLevel.Debug, "Need to start new job")]
    static partial void NeedToStartJob(this ILogger logger);
}
