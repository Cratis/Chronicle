// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Jobs;
using Microsoft.Extensions.Logging;

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

        var runningJobs = await _jobStorage!.GetJobs(JobStatus.Running, JobStatus.Preparing, JobStatus.PreparingSteps);
        foreach (var runningJob in runningJobs)
        {
            var grainType = (Type)runningJob.Type;
            var job = GrainFactory.GetGrain(grainType, runningJob.Id, new JobKey(_key.EventStore, _key.Namespace)) as IJob;
            await job!.Resume();
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

        var jobState = await _jobStorage!.GetJob(jobId);
        var job = (GrainFactory.GetGrain(jobState.Type, jobId, new JobKey(_key.EventStore, _key.Namespace)) as IJob)!;
        await job.Resume();
    }

    /// <inheritdoc/>
    public async Task Stop(JobId jobId)
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.StoppingJob(jobId);

        var jobState = await _jobStorage!.GetJob(jobId);
        var job = (GrainFactory.GetGrain(jobState.Type, jobId, new JobKey(_key.EventStore, _key.Namespace)) as IJob)!;
        await job.Stop();
    }

    /// <inheritdoc/>
    public async Task Delete(JobId jobId)
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.DeletingJob(jobId);

        await Stop(jobId);
        await _jobStepStorage!.RemoveAllForJob(jobId);
        await _jobStorage!.Remove(jobId);
    }

    /// <inheritdoc/>
    public Task OnCompleted(JobId jobId, JobStatus status)
    {
        using var scope = logger.BeginJobsManagerScope(_key);

        logger.JobCompleted(jobId, status);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<JobState>> GetJobsOfType<TJob, TRequest>()
        where TJob : IJob<TRequest>
        where TRequest : class =>
        await _namespaceStorage!.Jobs.GetJobs<TJob, JobState>();
}
