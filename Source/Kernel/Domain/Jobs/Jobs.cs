// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Domain.Jobs;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/jobs")]
public class Jobs : ControllerBase
{
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Jobs"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> to work with grains.</param>
    public Jobs(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
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
        var jobsManager = _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(microserviceId, tenantId));
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
        var jobsManager = _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(microserviceId, tenantId));
        await jobsManager.Delete(jobId);
    }
}
