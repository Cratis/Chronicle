// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for <see cref="IJobsManager"/>.
/// </summary>
public static class JobsManagerExtensions
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
    /// Gets the jobs of one type that are outstanding: unfinished, or removing with work still in flight.
    /// </summary>
    /// <param name="jobsManager">The <see cref="IJobsManager"/> to ask.</param>
    /// <typeparam name="TJob">The type of the job.</typeparam>
    /// <returns>The outstanding <see cref="JobState"/> of that type.</returns>
    /// <remarks>
    /// Jobs of other types are not read. A job holds its whole step plan, which for a job over thousands of partitions is
    /// megabytes, so listing every job to find the few of one type reads, copies and discards all of it on every call.
    /// </remarks>
    public static Task<IImmutableList<JobState>> GetOutstandingJobsOf<TJob>(this IJobsManager jobsManager) =>
        ListOf<TJob>(jobsManager, _outstandingStatuses, jobsManager.GetOutstandingJobs);

    /// <summary>
    /// Gets the jobs of one type that are unfinished.
    /// </summary>
    /// <param name="jobsManager">The <see cref="IJobsManager"/> to ask.</param>
    /// <typeparam name="TJob">The type of the job.</typeparam>
    /// <returns>The unfinished <see cref="JobState"/> of that type.</returns>
    public static Task<IImmutableList<JobState>> GetUnfinishedJobsOf<TJob>(this IJobsManager jobsManager) =>
        ListOf<TJob>(jobsManager, _unfinishedStatuses, jobsManager.GetUnfinishedJobs);

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
    /// <param name="maxJobsToDrop">The most stopped jobs that were never prepared to delete instead of resuming them; 0 drops none. Cratis.Choreography refuses to resume them, so they would block the work for good. When any exist, up to the allowed number is dropped and no job is resumed or started in this call; the next call looks the jobs up again.</param>
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
    /// <param name="maxJobsToDrop">The most stopped jobs that were never prepared to delete instead of resuming them; 0 drops none. Cratis.Choreography refuses to resume them, so they would block the work for good. When any exist, up to the allowed number is dropped and no job is resumed or started in this call; the next call looks the jobs up again.</param>
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
    /// <param name="stampBeforeStart">The optional callback granting and persisting the claim a new job runs under, returning the request stamped with its epoch. Called after <paramref name="onStartNew"/>, right before the job is started.</param>
    /// <param name="isObsolete">The optional check for a request carrying an ownership epoch its claim no longer holds. A stopped job running such a request is refused when resumed, so it is passed over rather than resumed.</param>
    /// <param name="canAcquire">The optional admission check repeated after awaited lookup and preparation.</param>
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
        Action<int>? onJobsDropped,
        Func<TRequest, Task<TRequest>>? stampBeforeStart = null,
        Func<IObserverJobRequest, bool>? isObsolete = null,
        Func<bool>? canAcquire = null)
        where TJob : IJob<TRequest>
        where TRequest : class, IObserverJobRequest
    {
        var starter = new ObserverJobStarter<TJob, TRequest>
        {
            JobsManager = jobsManager,
            Logger = logger,
            CreateRequest = createRequest,
            RequestPredicate = requestPredicate ?? (_ => true),
            OnAlreadyRunningJob = onAlreadyRunningJob ?? (() => Task.CompletedTask),
            OnResume = onResume ?? (() => Task.CompletedTask),
            OnStartNew = onStartNew ?? (() => Task.CompletedTask),
            OnResumeRefused = onResumeRefused ?? (() => Task.CompletedTask),
            ConcludedJobs = concludedJobs,
            MaxJobsToDrop = maxJobsToDrop,
            OnJobsDropped = onJobsDropped,
            StampBeforeStart = stampBeforeStart,
            IsObsolete = isObsolete,
            CanAcquire = canAcquire ?? (() => true)
        };
        return await starter.Run();
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
        (job.IsPreparingOrRunning || job.Status == JobStatus.Removing) && !JobLiveness.HasConcludedAllStepsLongAgo(job, now) && !JobLiveness.PreparationAbandoned(job, now);

    /// <summary>
    /// Gets whether a job ever reached its steps. Mirrors Job.JobIsPrepared in Cratis.Choreography: a stopped job that
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
    /// <param name="observerState">The optional observer state holding the persisted ownership claims. When given, a stopped job whose request carries an epoch its claim no longer holds owns nothing: it is refused when resumed.</param>
    /// <typeparam name="TRequest">The partition job request kind.</typeparam>
    /// <returns>The held partition keys.</returns>
    internal static ISet<Key> PartitionsHeldBy<TRequest>(this IEnumerable<JobState> jobs, ObserverKey observerKey, DateTimeOffset now, ObserverState? observerState = null)
        where TRequest : ObserverPartitionedJobRequest =>
        jobs.Where(job =>
            job.Request is TRequest request &&
            request.ObserverKey == observerKey &&
            !job.Status.HasFinished() &&
            !JobLiveness.HasConcludedAllStepsLongAgo(job, now) &&
            !IsStoppedUnderObsoleteClaim(job, request, observerState))
            .Select(job => ((TRequest)job.Request).Key)
            .ToHashSet();

    static bool IsStoppedUnderObsoleteClaim(JobState job, IObserverJobRequest request, ObserverState? observerState) =>
        observerState is not null && job.Status == JobStatus.Stopped && ObserverJobScope.IsObsolete(request, observerState);

    static Task<IImmutableList<JobState>> ListOf<TJob>(IJobsManager jobsManager, JobStatus[] statuses, Func<Task<IImmutableList<JobState>>> listAll)
    {
        // A job type that is not known cannot be narrowed to, so everything is listed, as it always was.
        var jobTypes = JobTypes.Instance;
        if (jobTypes is null || !jobTypes.GetFor(typeof(TJob)).TryPickT0(out var jobType, out _))
        {
            return listAll();
        }

        return jobsManager.GetJobs(new JobQuery { Type = jobType, Statuses = statuses, Take = 0 });
    }
}
