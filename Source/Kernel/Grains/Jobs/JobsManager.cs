// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobsManager"/>.
/// </summary>
public class JobsManager : Grain, IJobsManager
{
    readonly IClusterStorage _clusterStorage;
    readonly ILogger<JobsManager> _logger;
    IEventStoreNamespaceStorage? _namespaceStorage;
    IJobStorage? _jobStorage;
    IJobStepStorage? _jobStepStorage;
    JobsManagerKey _key = JobsManagerKey.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsManager"/> class.
    /// </summary>
    /// <param name="clusterStorage"><see cref="IClusterStorage"/> for working with underlying storage.</param>
    /// <param name="logger">Logger for logging.</param>
    public JobsManager(
        IClusterStorage clusterStorage,
        ILogger<JobsManager> logger)
    {
        _clusterStorage = clusterStorage;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this.GetPrimaryKeyLong(out var key);
        _key = key;

        _namespaceStorage = _clusterStorage.GetEventStore((string)_key.MicroserviceId).GetNamespace(_key.TenantId);
        _jobStorage = _namespaceStorage.Jobs;
        _jobStepStorage = _namespaceStorage.JobSteps;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        using var scope = _logger.BeginJobsManagerScope(_key);

        _logger.Rehydrating();

        var runningJobs = await _jobStorage!.GetJobs(JobStatus.Running, JobStatus.Preparing, JobStatus.PreparingSteps);
        foreach (var runningJob in runningJobs)
        {
            var grainType = (Type)runningJob.Type;
            var job = GrainFactory.GetGrain(grainType, runningJob.Id, new JobKey(_key.MicroserviceId, _key.TenantId)) as IJob;
            await job!.Resume();
        }
    }

    /// <inheritdoc/>
    public async Task Start<TJob, TRequest>(JobId jobId, TRequest request)
        where TJob : IJob<TRequest>
        where TRequest : class
    {
        using var scope = _logger.BeginJobsManagerScope(_key);

        _logger.StartingJob(jobId);

        var job = GrainFactory.GetGrain<TJob>(
            jobId,
            new JobKey(
                _key.MicroserviceId,
                _key.TenantId));

        await job.Start(request);
    }

    /// <inheritdoc/>
    public async Task Resume(JobId jobId)
    {
        using var scope = _logger.BeginJobsManagerScope(_key);

        _logger.ResumingJob(jobId);

        var jobState = await _jobStorage!.GetJob(jobId);
        var job = (GrainFactory.GetGrain(jobState.Type, jobId, new JobKey(_key.MicroserviceId, _key.TenantId)) as IJob)!;
        await job.Resume();
    }

    /// <inheritdoc/>
    public async Task Stop(JobId jobId)
    {
        using var scope = _logger.BeginJobsManagerScope(_key);

        _logger.StoppingJob(jobId);

        var jobState = await _jobStorage!.GetJob(jobId);
        var job = (GrainFactory.GetGrain(jobState.Type, jobId, new JobKey(_key.MicroserviceId, _key.TenantId)) as IJob)!;
        await job.Stop();
    }

    /// <inheritdoc/>
    public async Task Delete(JobId jobId)
    {
        using var scope = _logger.BeginJobsManagerScope(_key);

        _logger.DeletingJob(jobId);

        await Stop(jobId);
        await _jobStepStorage!.RemoveAllForJob(jobId);
        await _jobStorage!.Remove(jobId);
    }

    /// <inheritdoc/>
    public Task OnCompleted(JobId jobId, JobStatus status)
    {
        using var scope = _logger.BeginJobsManagerScope(_key);

        _logger.JobCompleted(jobId, status);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<JobState>> GetJobsOfType<TJob, TRequest>()
        where TJob : IJob<TRequest>
        where TRequest : class =>
        await _namespaceStorage!.Jobs.GetJobs<TJob, JobState>();
}
