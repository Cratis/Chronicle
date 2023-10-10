// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Defines a system that manages jobs.
/// </summary>
public interface IJobsManager : IGrainWithIntegerKey
{
    /// <summary>
    /// Start a job.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> for the job.</param>
    /// <param name="tenantId"><see cref="TenantId"/> for the job.</param>
    /// <param name="jobId">The <see cref="JobId"/> uniquely identifying the job.</param>
    /// <param name="request">The request parameter being passed to the job.</param>
    /// <typeparam name="TJob">Type of job to start.</typeparam>
    /// <typeparam name="TRequest">Type of the request to pass along.</typeparam>
    /// <returns>Awaitable task.</returns>
    Task Start<TJob, TRequest>(MicroserviceId microserviceId, TenantId tenantId, JobId jobId, TRequest request)
        where TJob : IJob<TRequest>;

    /// <summary>
    /// Report back completion of a job.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> for the job.</param>
    /// <param name="tenantId"><see cref="TenantId"/> for the job.</param>
    /// <param name="jobId">The identifier of the job completed.</param>
    /// <returns>Awaitable task.</returns>
    Task OnCompleted(MicroserviceId microserviceId, TenantId tenantId, JobId jobId);
}
