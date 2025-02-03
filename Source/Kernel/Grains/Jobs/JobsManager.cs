// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobsManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobsManager"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
/// <param name="jobTypes"><see cref="IJobTypes"/> that knows about job type associations.</param>
/// <param name="logger">Logger for logging.</param>
public class JobsManager(
    IStorage storage,
    IJobTypes jobTypes,
    ILogger<JobsManager> logger) : Grain, IJobsManager
{
    IEventStoreNamespaceStorage? _namespaceStorage;
    IJobStorage? _jobStorage;
    IJobStepStorage? _jobStepStorage;
    JobsManagerKey _key = JobsManagerKey.NotSet;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this.GetPrimaryKeyLong(out var key);
        _key = key;

        _namespaceStorage = storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace);
        _jobStorage = _namespaceStorage.Jobs;
        _jobStepStorage = _namespaceStorage.JobSteps;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.Rehydrating();
        var getRunningJobs = await _jobStorage!.GetJobs(JobStatus.Running, JobStatus.PreparingSteps, JobStatus.PreparingStepsForRunning);
        await getRunningJobs.Match(RehydrateJobs, HandleUnknownFailure);
        return;

        Task RehydrateJobs(IEnumerable<JobState> runningJobs)
        {
            var tasks = runningJobs.Select(_ => _.Id).Select(async jobId =>
            {
                try
                {
                    await Resume(jobId);
                }
                catch (Exception ex)
                {
                    logger.ErrorResumingJob(ex, jobId);
                }
            });
            return Task.WhenAll(tasks);
        }
    }

    /// <inheritdoc/>
    public async Task Start<TJob, TRequest>(JobId jobId, TRequest request)
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

        await job.Start(request);
    }

    /// <inheritdoc/>
    public async Task Resume(JobId jobId)
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.ResumingJob(jobId);

        var jobStateResult = await _jobStorage!.GetJob(jobId);
        await jobStateResult.Match(
            async jobState =>
            {
                var resumeJobResult = await ResumeJob(jobState);
                await HandleResumeJobResult(resumeJobResult);
            },
            error => HandleError(jobId, error),
            error => HandleUnknownFailure(jobId, error));
        return;

        async Task HandleResumeJobResult(OneOf<Result<ResumeJobSuccess, ResumeJobError>, Storage.Jobs.JobError> resumeJobResult)
        {
            await resumeJobResult.Match(
                resumeResult => resumeResult.Match(
                    success =>
                    {
                        switch (success)
                        {
                            case ResumeJobSuccess.JobAlreadyRunning:
                                logger.CannotResumeJobBecauseAlreadyRunning(jobId);
                                break;
                            case ResumeJobSuccess.JobCannotBeResumed:
                                logger.CannotResumeJob(jobId);
                                break;
                        }
                        return Task.CompletedTask;
                    },
                    resumeError => resumeError.Match<Task>(
                            jobError => HandleError(jobId, jobError),
                            failedResumingSteps =>
                            {
                                logger.FailedToResumeJobSteps(jobId, failedResumingSteps.FailedJobSteps);
                                return Task.CompletedTask;
                            })),
                jobError => HandleError(jobId, jobError));
        }
        Task<OneOf<Result<ResumeJobSuccess, ResumeJobError>, Storage.Jobs.JobError>> ResumeJob(JobState jobState) => GetJobGrain(jobState)
            .Match<Task<OneOf<Result<ResumeJobSuccess, ResumeJobError>, Storage.Jobs.JobError>>>(
                async job =>
                {
                    var result = await job.Resume();
                    return result.Match<OneOf<Result<ResumeJobSuccess, ResumeJobError>, Storage.Jobs.JobError>>(
                        success => Result<ResumeJobSuccess, ResumeJobError>.Success(success),
                        error => Result<ResumeJobSuccess, ResumeJobError>.Failed(error));
                },
                _ => Task.FromResult<OneOf<Result<ResumeJobSuccess, ResumeJobError>, Storage.Jobs.JobError>>(Storage.Jobs.JobError.TypeIsNotAssociatedWithAJobType));
    }

    /// <inheritdoc/>
    public async Task Stop(JobId jobId)
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        var stopJobResult = await TryStopJob(jobId);
        await stopJobResult.Match(
            _ => Task.CompletedTask,
            jobError => HandleError(jobId, jobError),
            jobStorageError => HandleError(jobId, jobStorageError),
            error => HandleUnknownFailure(jobId, error));
    }

    /// <inheritdoc/>
    public async Task Delete(JobId jobId)
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.DeletingJob(jobId);

        var stopJobResult = await TryStopJob(jobId);
        var stoppedJob = await stopJobResult.Match(
            _ => Task.FromResult(true),
            async jobError =>
            {
                await HandleError(jobId, jobError);
                return false;
            },
            async jobStorageError =>
            {
                await HandleError(jobId, jobStorageError);
                return false;
            },
            async error =>
            {
                await HandleUnknownFailure(jobId, error);
                return false;
            });
        if (!stoppedJob)
        {
            logger.FailedToStopJob(jobId);
            return;
        }
        await HandleCatch(_jobStepStorage!.RemoveAllForJob(jobId), jobId);
        await HandleCatch(_jobStorage!.Remove(jobId), jobId);
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

    Result<IJob, IJobTypes.GetClrTypeForError> GetJobGrain(JobState jobState) => jobTypes.GetClrTypeFor(jobState.Type)
        .Match<Result<IJob, IJobTypes.GetClrTypeForError>>(
            jobType => Result<IJob, IJobTypes.GetClrTypeForError>.Success((IJob)GrainFactory.GetGrain(jobType, jobState.Id, new JobKey(_key.EventStore, _key.Namespace))),
            error => error)!;

    async Task<OneOf<None, JobError, Storage.Jobs.JobError, Exception>> TryStopJob(JobId jobId)
    {
        logger.StoppingJob(jobId);

        var jobStateResult = await _jobStorage!.GetJob(jobId);
        return await jobStateResult.Match(
            async jobState =>
            {
                var stopJobResult = await StopJob(jobState);
                return stopJobResult.Match<OneOf<None, JobError, Storage.Jobs.JobError, Exception>>(
                    none => none,
                    jobError => jobError,
                    jobStorageError => jobStorageError);
            },
            error => Task.FromResult<OneOf<None, JobError, Storage.Jobs.JobError, Exception>>(error),
            ex => Task.FromResult<OneOf<None, JobError, Storage.Jobs.JobError, Exception>>(ex));

        Task<OneOf<None, JobError, Storage.Jobs.JobError>> StopJob(JobState jobState) => GetJobGrain(jobState)
            .Match<Task<OneOf<None, JobError, Storage.Jobs.JobError>>>(
                async job =>
                {
                    var result = await job.Stop();
                    return result.Match<OneOf<None, JobError, Storage.Jobs.JobError>>(
                        none => none,
                        error => error);
                },
                _ => Task.FromResult<OneOf<None, JobError, Storage.Jobs.JobError>>(Storage.Jobs.JobError.TypeIsNotAssociatedWithAJobType));
    }

    async Task HandleCatch(Task<Catch> doCatch, JobId jobId)
    {
        var result = await doCatch;
        await result.Match(
            _ => Task.CompletedTask,
            error => HandleUnknownFailure(jobId, error));
    }

    Task HandleError(JobId jobId, Storage.Jobs.JobError jobError)
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

    Task HandleError(JobId jobId, JobError jobError)
    {
        switch (jobError)
        {
            default:
                logger.JobErrorOccurred(jobId, jobError);
                break;
        }
        return Task.CompletedTask;
    }

    Task HandleUnknownFailure(JobId jobId, Exception ex)
    {
        logger.UnknownError(ex, jobId);

        // TODO: I'm not sure yet whether to throw or not.
        return Task.FromException(ex);
    }

    Task HandleUnknownFailure(Exception ex)
    {
        logger.UnknownError(ex);

        // TODO: I'm not sure yet whether to throw or not.
        return Task.FromException(ex);
    }
}
