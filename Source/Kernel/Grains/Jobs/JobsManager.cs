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
        var storage = ServiceProvider.GetRequiredService<IJobStorage<JobState<TRequest>>>();
        return await storage.GetJobs<TJob>();
    }
}
