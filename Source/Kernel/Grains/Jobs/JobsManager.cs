// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf.Types;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobsManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobsManager"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
/// <param name="jobTypes"><see cref="IJobTypes"/> that knows about job type associations.</param>
/// <param name="options">Chronicle options.</param>
/// <param name="logger">Logger for logging.</param>
[Reentrant]
public class JobsManager(
    IStorage storage,
    IJobTypes jobTypes,
    IOptions<ChronicleOptions> options,
    ILogger<JobsManager> logger) : Grain, IJobsManager
{
    IEventStoreNamespaceStorage? _namespaceStorage;
    IJobStorage? _jobStorage;
    IJobStepStorage? _jobStepStorage;
    JobsManagerKey _key = JobsManagerKey.NotSet;
    IGrainTimer? _cleanupTimer;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this.GetPrimaryKeyLong(out var key);
        _key = key!;

        _namespaceStorage = storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace);
        _jobStorage = _namespaceStorage.Jobs;
        _jobStepStorage = _namespaceStorage.JobSteps;

        var cleanupCadence = options.Value.Jobs.GetEffectiveCleanupCadence();
        _cleanupTimer = this.RegisterGrainTimer(
            async _ => await CleanupDeadJobs(),
            new GrainTimerCreationOptions
            {
                DueTime = cleanupCadence,
                Period = cleanupCadence
            });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _cleanupTimer?.Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<Result<JobId, StartJobError>> Start<TJob, TRequest>(TRequest request)
        where TJob : IJob<TRequest>
        where TRequest : class, IJobRequest
        => Start<TJob, TRequest>(JobId.New(), request);

    /// <inheritdoc/>
    public async Task<Result<JobId, StartJobError>> Start<TJob, TRequest>(JobId jobId, TRequest request)
        where TJob : IJob<TRequest>
        where TRequest : class, IJobRequest
    {
        using var scope = logger.BeginJobsManagerScope(_key);
        logger.StartingJob(jobId);

        var job = GrainFactory.GetGrain<TJob>(
            jobId,
            new JobKey(
                _key.EventStore,
                _key.Namespace));

        return (await job.Start(request)).Match<Result<JobId, StartJobError>>(_ => jobId, error => error);
    }

    /// <inheritdoc/>
    public async Task Resume(JobId jobId)
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.ResumingJob(jobId);

        await ResumeJobAndHandleResult(jobId);
    }

    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.Rehydrating();

        await CleanupDeadJobs();

        var getRunningJobs = await _jobStorage!.GetJobs(JobStatus.Running, JobStatus.PreparingJob, JobStatus.PreparingSteps, JobStatus.StartingSteps);
        await getRunningJobs.Match(RehydrateJobs, HandleUnknownFailure);
        return;

        Task RehydrateJobs(IEnumerable<JobState> runningJobs)
        {
            var tasks = runningJobs.Select(state => ResumeJobAndHandleResult(state.Id));
            return Task.WhenAll(tasks);
        }
    }

    /// <inheritdoc/>
    public async Task Stop(JobId jobId)
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.StoppingJob(jobId);
        _ = await DoActionOnJobGrain<None>(jobId, async (_, job) =>
        {
            var stopResult = await job.Stop();
            await stopResult.Match(
                none => Task.CompletedTask,
                error => HandleStopJobError(jobId, error));
            return default;
        });
    }

    /// <inheritdoc/>
    public async Task Delete(JobId jobId)
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.DeletingJob(jobId);
        var deletedJobResult = await DoActionOnJobGrain(jobId, async (jobState, job) =>
        {
            var removeResult = await job.Remove();
            return await removeResult.Match(
                _ => Task.FromResult(true),
                async removeJobError =>
                {
                    await HandleRemoveJobError(jobState.Id, removeJobError);
                    return false;
                });
        });
        if (!deletedJobResult.TryGetResult(out var deletedJob) || !deletedJob)
        {
            logger.FailedToDeleteJob(jobId);
        }
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<JobState>> GetJobsOfType<TJob, TRequest>()
        where TJob : IJob<TRequest>
        where TRequest : class, IJobRequest
    {
        var getJobs = await _namespaceStorage!.Jobs.GetJobs<TJob, JobState>();
        return await getJobs.Match(
            Task.FromResult,
            error =>
            {
                logger.UnableToGetJobs(typeof(TJob), error);
                return Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty);
            },
            async error =>
            {
                await HandleUnknownFailure(error);
                return [];
            });
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<JobState>> GetAllJobs()
    {
        var getJobs = await _jobStorage!.GetJobs();
        return await getJobs.Match(
            Task.FromResult,
            exception =>
            {
                logger.UnableToGetAllJobs(exception);
                return Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty);
            });
    }

    /// <inheritdoc/>
    public async Task CleanupDeadJobs()
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.CleaningUpDeadJobs();

        var threshold = options.Value.Jobs.GetEffectiveDeadJobThreshold();
        var cutoffTime = DateTimeOffset.UtcNow - threshold;

        var getPreparingJobs = await _jobStorage!.GetJobs(JobStatus.PreparingJob, JobStatus.PreparingSteps);
        await getPreparingJobs.Match(
            async preparingJobs => await CleanupDeadJobsInternal(preparingJobs, cutoffTime),
            exception =>
            {
                logger.FailedToGetJobsForCleanup(exception);
                return Task.CompletedTask;
            });
    }

    async Task CleanupDeadJobsInternal(IEnumerable<JobState> preparingJobs, DateTimeOffset cutoffTime)
    {
        var deadJobs = new List<JobState>();

        foreach (var job in preparingJobs.Where(j => j.Created < cutoffTime))
        {
            var stepCountResult = await _jobStepStorage!.CountForJob(job.Id);
            var shouldDelete = await stepCountResult.Match(
                count => Task.FromResult(count == 0),
                error =>
                {
                    logger.FailedToGetStepCountForJob(job.Id, error);
                    logger.SkippingJobDueToStepCountError(job.Id);
                    return Task.FromResult(false);
                });

            if (shouldDelete)
            {
                deadJobs.Add(job);
            }
        }

        if (deadJobs.Count > 0)
        {
            logger.FoundDeadJobs(deadJobs.Count);
            var deleteTasks = deadJobs.Select(job => Delete(job.Id)).ToList();
            await Task.WhenAll(deleteTasks);
        }
        else
        {
            logger.NoDeadJobsFound();
        }
    }

    Result<IJob, IJobTypes.GetClrTypeForError> GetJobGrain(JobState jobState) => jobTypes.GetClrTypeFor(jobState.Type)
        .Match(
            jobType => Result<IJob, IJobTypes.GetClrTypeForError>.Success((IJob)GrainFactory.GetGrain(jobType, jobState.Id, new JobKey(_key.EventStore, _key.Namespace))),
            error => error)!;

    async Task<Result<TResult, None>> DoActionOnJobGrain<TResult>(JobId jobId, Func<JobState, IJob, Task<TResult>> doAction)
    {
        try
        {
            var jobStateResult = await _jobStorage!.GetJob(jobId);
            var getJobState = await jobStateResult.Match(
                state => Task.FromResult(Result.Success<JobState, None>(state)),
                async jobError =>
                {
                    await HandleJobStorageError(jobId, jobError);
                    return Result<JobState, None>.Failed(default);
                },
                async exception =>
                {
                    await HandleUnknownFailure(jobId, exception);
                    return Result<JobState, None>.Failed(default);
                });
            if (!getJobState.TryGetResult(out var jobState))
            {
                return Result<TResult, None>.Failed(default);
            }
            var getJobGrain = GetJobGrain(jobState);
            if (getJobGrain.TryPickT1(out var getJobGrainError, out var job))
            {
                await HandleGetJobGrainError(jobState.Id, jobState.Type, getJobGrainError);
                return Result<TResult, None>.Failed(default);
            }
            return await doAction(jobState, job);
        }
        catch (Exception ex)
        {
            logger.UnknownError(ex);
            return Result<TResult, None>.Failed(default);
        }
    }

    async Task ResumeJobAndHandleResult(JobId jobId)
    {
        _ = await DoActionOnJobGrain(jobId, async (_, job) =>
        {
            var resumeResult = await job.Resume();
            await resumeResult.Match(
                success => HandleResumeJobSuccess(jobId, success),
                error => HandleResumeJobError(jobId, error));
            return default(None);
        });
    }

    Task HandleRemoveJobError(JobId jobId, RemoveJobError jobError)
    {
        switch (jobError)
        {
            case RemoveJobError.JobIsCompleted:
                logger.JobIsCompletedAndCannotBeRemovedOrStopped(jobId);
                break;
            case RemoveJobError.JobIsAlreadyBeingRemoved:
                logger.JobIsAlreadyBeingRemoved(jobId);
                break;
            default:
                logger.FailedToRemoveJob(jobId);
                break;
        }
        return Task.CompletedTask;
    }
    Task HandleStopJobError(JobId jobId, StopJobError jobError)
    {
        switch (jobError)
        {
            case StopJobError.JobIsCompleted:
                logger.JobIsCompletedAndCannotBeRemovedOrStopped(jobId);
                break;
            case StopJobError.JobIsNotRunning:
                logger.JobCannotBeStoppedIsNotRunning(jobId);
                break;
            default:
                logger.FailedToStopJob(jobId);
                break;
        }
        return Task.CompletedTask;
    }

    Task HandleGetJobGrainError(JobId jobId, JobType jobType, IJobTypes.GetClrTypeForError error)
    {
        logger.UnableToGetJob(jobId, jobType, error);
        return Task.CompletedTask;
    }

    Task HandleJobStorageError(JobId jobId, Storage.Jobs.JobError jobError)
    {
        switch (jobError)
        {
            case Storage.Jobs.JobError.NotFound:
                logger.JobCouldNotBeFound(jobId);
                break;
            default:
                logger.JobErrorOccurred(jobId, jobError);
                break;
        }
        return Task.CompletedTask;
    }

    Task HandleResumeJobSuccess(JobId jobId, ResumeJobSuccess success)
    {
        switch (success)
        {
            case ResumeJobSuccess.JobAlreadyRunning:
                logger.CannotResumeJobBecauseAlreadyRunning(jobId);
                break;
            case ResumeJobSuccess.JobIsCompleted:
                logger.CannotResumeJobBecauseCompleted(jobId);
                break;
            default:
                return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    Task HandleResumeJobError(JobId jobId, ResumeJobError error) => error.Match(
        cannotResumeError => HandleCannotResumeError(jobId, cannotResumeError),
        failedResumingSteps =>
        {
            logger.FailedToResumeJobSteps(jobId, failedResumingSteps.FailedJobSteps);
            return Task.CompletedTask;
        });

    Task HandleCannotResumeError(JobId jobId, CannotResumeJobError jobError)
    {
        switch (jobError)
        {
            case CannotResumeJobError.JobIsNotPrepared:
                logger.CannotResumeUnpreparedJob(jobId);
                break;
            case CannotResumeJobError.JobCannotBeResumed:
                logger.JobCannotBeResumed(jobId);
                break;
            default:
                logger.FailedResumingJob(jobId);
                break;
        }
        return Task.CompletedTask;
    }

    Task HandleUnknownFailure(JobId jobId, Exception ex)
    {
        logger.UnknownError(ex, jobId);
        return Task.FromException(ex);
    }

    Task HandleUnknownFailure(Exception ex)
    {
        logger.UnknownError(ex);
        return Task.FromException(ex);
    }
}
