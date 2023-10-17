// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Kernel.Persistence.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobsManager"/>.
/// </summary>
public class JobsManager : Grain, IJobsManager
{
    JobsManagerKey _key = JobsManagerKey.NotSet;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobsManager"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public JobsManager(IExecutionContextManager executionContextManager)
    {
        _executionContextManager = executionContextManager;
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
