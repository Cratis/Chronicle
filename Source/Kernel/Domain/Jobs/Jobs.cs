// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Jobs;
using Cratis.Kernel.Grains.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Kernel.Domain.Jobs;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Jobs"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to work with grains.</param>
[Route("/api/events/store/{microserviceId}/{tenantId}/jobs")]
public class Jobs(IGrainFactory grainFactory) : ControllerBase
{
    /// <summary>
    /// Stop a specific job.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> the job is for.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> the job is for.</param>
    /// <param name="jobId"><see cref="JobId"/> to stop.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/resume/")]
    public async Task ResumeJob(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] JobId jobId)
    {
        var jobsManager = grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(microserviceId, tenantId));
        await jobsManager.Resume(jobId);
    }

    /// <summary>
    /// Stop a specific job.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> the job is for.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> the job is for.</param>
    /// <param name="jobId"><see cref="JobId"/> to stop.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/stop/")]
    public async Task StopJob(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] JobId jobId)
    {
        var jobsManager = grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(microserviceId, tenantId));
        await jobsManager.Stop(jobId);
    }

    /// <summary>
    /// Delete a specific job.
    /// </summary>
    /// <param name="microserviceId">The <see cref="MicroserviceId"/> the job is for.</param>
    /// <param name="tenantId">The <see cref="TenantId"/> the job is for.</param>
    /// <param name="jobId"><see cref="JobId"/> to stop.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/delete/")]
    public async Task DeleteJob(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] JobId jobId)
    {
        var jobsManager = grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(microserviceId, tenantId));
        await jobsManager.Delete(jobId);
    }
}
