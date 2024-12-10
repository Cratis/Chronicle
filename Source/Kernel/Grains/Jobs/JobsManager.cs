// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;
using OneOf.Types;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobsManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobsManager"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
/// <param name="logger">Logger for logging.</param>
public class JobsManager(
    IStorage storage,
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
            var tasks = runningJobs.Select(_ => (_.Id, GetJobGrain(_))).Select(async idAndJob =>
            {
                var (id, job) = idAndJob;
                try
                {
                    await job.Resume();
                }
                catch (Exception ex)
                {
                    logger.ErrorResumingJob(ex, id);
                }
            });
            return Task.WhenAll(tasks);
        }
    }

    /// <inheritdoc/>
    public async Task Start<TJob, TRequest>(JobId jobId, TRequest request)
        where TJob : IJob<TRequest>
        where TRequest : class
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
        await jobStateResult.Match(ResumeJob, error => HandleError(jobId, error), error => HandleUnknownFailure(jobId, error));
        return;

        Task ResumeJob(JobState jobState)
        {
            var job = GetJobGrain(jobState);
            return job.Resume();
        }
    }

    /// <inheritdoc/>
    public async Task Stop(JobId jobId)
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        var stopJobResult = await TryStopJob(jobId);
        await stopJobResult.Match(_ => Task.CompletedTask, error => HandleError(jobId, error), error => HandleUnknownFailure(jobId, error));
    }

    /// <inheritdoc/>
    public async Task Delete(JobId jobId)
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.DeletingJob(jobId);

        var stopJobResult = await TryStopJob(jobId);
        var stoppedJob = await stopJobResult.Match(
            _ => Task.FromResult(true),
            async error =>
            {
                await HandleError(jobId, error);
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
        where TRequest : class
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

    IJob GetJobGrain(JobState jobState) => (GrainFactory.GetGrain(
        jobState.Type,
        jobState.Id,
        new JobKey(_key.EventStore, _key.Namespace)) as IJob)!;

    async Task<Catch<None, Storage.Jobs.JobError>> TryStopJob(JobId jobId)
    {
        logger.StoppingJob(jobId);

        var jobStateResult = await _jobStorage!.GetJob(jobId);
        return await jobStateResult.Match(
            async jobState =>
            {
                try
                {
                    await StopJob(jobState);
                    return Catch.Success<None, Storage.Jobs.JobError>(default);
                }
                catch (Exception ex)
                {
                    return ex;
                }
            },
            error => Task.FromResult<Catch<None, Storage.Jobs.JobError>>(error),
            error => Task.FromResult<Catch<None, Storage.Jobs.JobError>>(error));

        Task StopJob(JobState jobState)
        {
            var job = GetJobGrain(jobState);
            return job.Stop();
        }
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
