// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Persistence.Jobs;
using Aksio.DependencyInversion;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobsManager"/>.
/// </summary>
public class JobsManager : Grain, IJobsManager
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IJobStorage> _jobStorageProvider;
    readonly ProviderFor<IJobStepStorage> _jobStepStorageProvider;
    JobsManagerKey _key = JobsManagerKey.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsManager"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jobStorageProvider">Provider for <see cref="IJobStorage"/>.</param>
    /// <param name="jobStepStorageProvider">Provider for <see cref="IJobStepStorage"/>.</param>
    public JobsManager(
        IExecutionContextManager executionContextManager,
        ProviderFor<IJobStorage> jobStorageProvider,
        ProviderFor<IJobStepStorage> jobStepStorageProvider)
    {
        _executionContextManager = executionContextManager;
        _jobStorageProvider = jobStorageProvider;
        _jobStepStorageProvider = jobStepStorageProvider;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this.GetPrimaryKeyLong(out var key);
        _key = key;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Start<TJob, TRequest>(JobId jobId, TRequest request)
        where TJob : IJob<TRequest>
    {
        _executionContextManager.Establish(_key.TenantId, _executionContextManager.Current.CorrelationId, _key.MicroserviceId);
        var job = GrainFactory.GetGrain<TJob>(
            jobId,
            new JobKey(
                _key.MicroserviceId,
                _key.TenantId));

        return job.Start(request);
    }

    /// <inheritdoc/>
    public Task Cancel(JobId jobId)
    {
        // TODO: Actual cancel the job - if it is running
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Delete(JobId jobId)
    {
        _executionContextManager.Establish(_key.TenantId, _executionContextManager.Current.CorrelationId, _key.MicroserviceId);
        await _jobStepStorageProvider().RemoveAllForJob(jobId);
        await _jobStorageProvider().Remove(jobId);
    }

    /// <inheritdoc/>
    public Task OnCompleted(JobId jobId)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<JobState<TRequest>>> GetJobsOfType<TJob, TRequest>()
        where TJob : IJob<TRequest>
        where TRequest : class
    {
        _executionContextManager.Establish(_key.TenantId, _executionContextManager.Current.CorrelationId, _key.MicroserviceId);
        var storage = ServiceProvider.GetRequiredService<IJobStorage<JobState<TRequest>>>();
        return await storage.GetJobs<TJob>();
    }
}
