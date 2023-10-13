// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobsManager"/>.
/// </summary>
public class JobsManager : Grain, IJobsManager
{
    /// <inheritdoc/>
    public Task Start<TJob, TRequest>(MicroserviceId microserviceId, TenantId tenantId, JobId jobId, TRequest request)
        where TJob : IJob<TRequest>
    {
        var job = GrainFactory.GetGrain<TJob>(
            jobId,
            new JobKey(
                microserviceId,
                tenantId));

        return job.Start(request);
    }

    /// <inheritdoc/>
    public Task OnCompleted(MicroserviceId microserviceId, TenantId tenantId, JobId jobId)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IImmutableList<TRequest>> GetRunningJobsOfType<TJob, TRequest>()
        where TJob : IJob<TRequest> => throw new NotImplementedException();
}
