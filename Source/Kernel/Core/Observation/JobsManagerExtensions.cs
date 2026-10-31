// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;
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

    static readonly JobStatus[] _outstandingStatuses = [.. _unfinishedStatuses, JobStatus.Removing];

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
    /// Gets jobs that still own work, including jobs draining during removal.
    /// </summary>
    /// <param name="jobsManager">The jobs manager.</param>
    /// <returns>The outstanding jobs.</returns>
    public static Task<IImmutableList<JobState>> GetOutstandingJobs(this IJobsManager jobsManager) =>
        jobsManager.GetJobs(new JobQuery { Statuses = _outstandingStatuses, Take = 0 });

    /// <summary>
    /// Gets outstanding jobs, preserving lookup failures so they cannot be mistaken for no jobs,
    /// unlike the jobs manager lookup which returns an empty list on failure.
    /// </summary>
    /// <param name="jobStorage">The job storage.</param>
    /// <returns>The outstanding jobs or the lookup failure.</returns>
    public static Task<Catch<IImmutableList<JobState>>> GetOutstandingJobs(this IJobStorage jobStorage) =>
        jobStorage.GetJobs(new JobQuery { Statuses = _outstandingStatuses, Take = 0 });

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
    /// <param name="concludedJobs">The optional jobs that have already reported their work as done and therefore can not own it, even while still finalizing. Read after the lookup and never changed; forgetting them is up to their owner.</param>
    /// <param name="maxJobsToDrop">The most stopped jobs that were never prepared to delete instead of resuming them; 0 drops none. Cratis.Orleans refuses to resume them, so they would block the work for good. When any exist, up to the allowed number is dropped and no job is resumed or started in this call; the next call looks the jobs up again.</param>
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
        ICollection<JobId>? concludedJobs = null,
        int maxJobsToDrop = 0)
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
            concludedJobs,
            maxJobsToDrop);

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
    /// <param name="concludedJobs">The optional jobs that have already reported their work as done and therefore can not own it, even while still finalizing. Read after the lookup and never changed; forgetting them is up to their owner.</param>
    /// <param name="maxJobsToDrop">The most stopped jobs that were never prepared to delete instead of resuming them; 0 drops none. Cratis.Orleans refuses to resume them, so they would block the work for good. When any exist, up to the allowed number is dropped and no job is resumed or started in this call; the next call looks the jobs up again.</param>
    /// <typeparam name="TJob">The type of the job.</typeparam>
    /// <typeparam name="TRequest">The type of the observer request.</typeparam>
    /// <returns>The <see cref="JobId"/> of the running, resumed, or newly started job; or <see cref="JobId.NotSet"/> if no job could be started.</returns>
    /// <remarks>
    /// A job excluded because it concluded while the lookup was in flight has moved the observer on. A request built
    /// before the lookup still carries the position from before that job's work, and a replacement started from it
    /// delivers that work a second time.
    /// </remarks>
    public static Task<JobId> StartOrResumeObserverJobFor<TJob, TRequest>(
        this IJobsManager jobsManager,
        ILogger logger,
        Func<TRequest> createRequest,
        Func<TRequest, bool>? requestPredicate = null,
        Func<Task>? onAlreadyRunningJob = null,
        Func<Task>? onResume = null,
        Func<Task>? onStartNew = null,
        Func<Task>? onResumeRefused = null,
        ICollection<JobId>? concludedJobs = null,
        int maxJobsToDrop = 0)
        where TJob : IJob<TRequest>
        where TRequest : class, IObserverJobRequest =>
        jobsManager.StartOrResumeObserverJobReportingDrops<TJob, TRequest>(
            logger,
            createRequest,
            requestPredicate,
            onAlreadyRunningJob,
            onResume,
            onStartNew,
            onResumeRefused,
            concludedJobs,
            maxJobsToDrop,
            null);

    /// <summary>
    /// Starts or resumes an observer job like the public overload, telling the caller how many jobs it deleted.
    /// </summary>
    /// <param name="jobsManager">The jobs manager.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="createRequest">Creates the observer request after the existing jobs have been looked up.</param>
    /// <param name="requestPredicate">The optional predicate.</param>
    /// <param name="onAlreadyRunningJob">The optional callback when there already is a running job.</param>
    /// <param name="onResume">The optional callback when a job needs to be resumed.</param>
    /// <param name="onStartNew">The optional callback when a new job needs to be started.</param>
    /// <param name="onResumeRefused">The optional callback when a stopped job was found but refused to resume, so nothing owns the work.</param>
    /// <param name="concludedJobs">The optional jobs that have already reported their work as done.</param>
    /// <param name="maxJobsToDrop">The most never-prepared stopped jobs to delete; 0 drops none.</param>
    /// <param name="onJobsDropped">The optional callback with the number of jobs that were deleted, called before anything is resumed or started.</param>
    /// <typeparam name="TJob">The type of the job.</typeparam>
    /// <typeparam name="TRequest">The type of the observer request.</typeparam>
    /// <returns>The <see cref="JobId"/> of the running, resumed, or newly started job; or <see cref="JobId.NotSet"/> if no job could be started.</returns>
    internal static async Task<JobId> StartOrResumeObserverJobReportingDrops<TJob, TRequest>(
        this IJobsManager jobsManager,
        ILogger logger,
        Func<TRequest> createRequest,
        Func<TRequest, bool>? requestPredicate,
        Func<Task>? onAlreadyRunningJob,
        Func<Task>? onResume,
        Func<Task>? onStartNew,
        Func<Task>? onResumeRefused,
        ICollection<JobId>? concludedJobs,
        int maxJobsToDrop,
        Action<int>? onJobsDropped)
        where TJob : IJob<TRequest>
        where TRequest : class, IObserverJobRequest
    {
        requestPredicate ??= _ => true;
        onAlreadyRunningJob ??= () => Task.CompletedTask;
        onResume ??= () => Task.CompletedTask;
        onStartNew ??= () => Task.CompletedTask;
        onResumeRefused ??= () => Task.CompletedTask;
        var jobs = await jobsManager.GetOutstandingJobs();
        var request = createRequest();

        // The exclusion is read after the lookup, so it covers jobs that concluded while it was in flight. A listing is no
        // evidence that a concluded job has finished - the jobs manager reports a failed lookup as an empty listing - so
        // nothing is forgotten here. The filter runs to completion before anything else is awaited.
        jobs = jobs.Where(job =>
            job.Request is TRequest observerRequest &&
            observerRequest.ObserverKey == request.ObserverKey &&
            requestPredicate(observerRequest) &&
            !(concludedJobs?.Contains(job.Id) ?? false)).ToImmutableList();
        var now = DateTimeOffset.UtcNow;
        var alreadyRunningJob = jobs.FirstOrDefault(job => job.IsLive(now));
        if (alreadyRunningJob is not null)
        {
            logger.FoundRunningJob(alreadyRunningJob.Id);
            await onAlreadyRunningJob.Invoke();
            return alreadyRunningJob.Id;
        }

        var pausedJobs = jobs.Where(job => job.Status == JobStatus.Stopped).ToList();
        if (maxJobsToDrop > 0 && !await jobsManager.TryDropJobsThatCanNeverResume(logger, pausedJobs, maxJobsToDrop, onJobsDropped))
        {
            // A job that can never resume blocks the work, or may still do so after a failed delete, so nothing is started until a later lookup lists none. The caller re-arms its own retry.
            return JobId.NotSet;
        }

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

                // A job that was stopped before it prepared any step has nothing to resume and never will, so every
                // later lookup would find it, refuse it again and leave the observer retrying catch-up in a tight
                // loop. It carries no work: remove it so the next catch-up starts a job of its own.
                if (JobLiveness.NeverPrepared(pausedJob))
                {
                    logger.RemovingUnresumableJob(pausedJob.Id);
                    await jobsManager.Delete(pausedJob.Id);
                }

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

    /// <summary>
    /// Gets whether a job is alive: preparing, running or removing with work still in flight.
    /// A removing job still drains and reports back when its workers stop, so it still owns its work.
    /// </summary>
    /// <param name="job">The <see cref="JobState"/> to check.</param>
    /// <param name="now">The current time.</param>
    /// <returns>True if the job can still be expected to move on its own.</returns>
    /// <remarks>
    /// A persisted status is a claim, not a proof. A job whose every step has concluded, but which has stayed recorded
    /// as running past <see cref="JobLiveness.StaleAfter"/>, lost the report that would have finished it - a restart, or
    /// a hung completion. Treating it as running told an operator a replay had started while nothing was created.
    /// </remarks>
    internal static bool IsLive(this JobState job, DateTimeOffset now) =>
        (job.IsPreparingOrRunning || job.Status == JobStatus.Removing) && !JobLiveness.HasConcludedAllStepsLongAgo(job, now);

    /// <summary>
    /// Gets whether a job ever reached its steps. Mirrors Job.JobIsPrepared in Cratis.Orleans: a stopped job that
    /// was never prepared is refused when resumed, so it can never run again.
    /// </summary>
    /// <param name="job">The <see cref="JobState"/> to check.</param>
    /// <returns>True if the job has started its steps.</returns>
    internal static bool WasEverPrepared(this JobState job) => job.StatusChanges.Any(_ => _.Status == JobStatus.StartingSteps);

    /// <summary>
    /// Gets whether a job with the given status has finished for good - excluding jobs still draining during removal.
    /// </summary>
    /// <param name="status">The <see cref="JobStatus"/> to check.</param>
    /// <returns>True if the job has finished; false if it is preparing, running, stopped or removing.</returns>
    internal static bool HasFinished(this JobStatus status) => !_outstandingStatuses.Contains(status);

    /// <summary>
    /// Finds partitions still owned by outstanding jobs of the requested kind and observer.
    /// </summary>
    /// <param name="jobs">The jobs to inspect.</param>
    /// <param name="observerKey">The observer that owns the work.</param>
    /// <param name="now">The current time.</param>
    /// <typeparam name="TRequest">The partition job request kind.</typeparam>
    /// <returns>The held partition keys.</returns>
    internal static ISet<Key> PartitionsHeldBy<TRequest>(this IEnumerable<JobState> jobs, ObserverKey observerKey, DateTimeOffset now)
        where TRequest : ObserverPartitionedJobRequest =>
        jobs.Where(job =>
            job.Request is TRequest request &&
            request.ObserverKey == observerKey &&
            !job.Status.HasFinished() &&
            !JobLiveness.HasConcludedAllStepsLongAgo(job, now))
            .Select(job => ((TRequest)job.Request).Key)
            .ToHashSet();

    /// <summary>
    /// Deletes up to the given number of stopped jobs that were never prepared.
    /// </summary>
    /// <param name="jobsManager">The jobs manager.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="pausedJobs">The stopped jobs.</param>
    /// <param name="maxJobsToDrop">The most jobs to delete.</param>
    /// <param name="onJobsDropped">The optional callback with the number of jobs deleted.</param>
    /// <returns>True if there was no job that can never resume, so the work can go on; false if any was found, whether or not it could be deleted.</returns>
    /// <remarks>
    /// Deleting returns normally even when the job could not be removed, and a lookup that fails reports an empty listing, so
    /// a deletion is never taken as proof. Once a job has been dropped the caller starts nothing; its next retry looks the
    /// jobs up afresh and goes on only when no such job is listed.
    /// </remarks>
    static async Task<bool> TryDropJobsThatCanNeverResume(this IJobsManager jobsManager, ILogger logger, List<JobState> pausedJobs, int maxJobsToDrop, Action<int>? onJobsDropped)
    {
        var unresumableJobs = pausedJobs.Where(job => !job.WasEverPrepared()).ToList();
        if (unresumableJobs.Count == 0)
        {
            return true;
        }

        var dropped = 0;
        foreach (var jobToDrop in unresumableJobs.Take(maxJobsToDrop))
        {
            try
            {
                logger.DroppingJobThatCanNeverResume(jobToDrop.Id);
                await jobsManager.Delete(jobToDrop.Id);
                dropped++;
            }
            catch (Exception ex)
            {
                logger.CouldNotDropJob(jobToDrop.Id, ex);
                break;
            }
        }

        onJobsDropped?.Invoke(dropped);
        return false;
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

    [LoggerMessage(LogLevel.Warning, "Dropping stopped job {JobId} that was never prepared and can never resume")]
    static partial void DroppingJobThatCanNeverResume(this ILogger logger, JobId jobId);

    [LoggerMessage(LogLevel.Warning, "Could not drop stopped job {JobId} that can never resume")]
    static partial void CouldNotDropJob(this ILogger logger, JobId jobId, Exception exception);

    [LoggerMessage(LogLevel.Warning, "Stopped job {JobId} never prepared a step and can not be resumed - removing it so catch-up can start a new job")]
    static partial void RemovingUnresumableJob(this ILogger logger, JobId jobId);
}
