// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Jobs;
using Cratis.Kernel.Grains.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Jobs.Commands;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Jobs"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to work with grains.</param>
[Route("/api/events/store/{eventStore}/{namespace}/jobs")]
public class Jobs(IGrainFactory grainFactory) : ControllerBase
{
    /// <summary>
    /// Stop a specific job.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId"><see cref="JobId"/> to stop.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/resume/")]
    public async Task ResumeJob(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid jobId)
    {
        var jobsManager = grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(eventStore, @namespace));
        await jobsManager.Resume(jobId);
    }

    /// <summary>
    /// Stop a specific job.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId"><see cref="JobId"/> to stop.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/stop/")]
    public async Task StopJob(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid jobId)
    {
        var jobsManager = grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(eventStore, @namespace));
        await jobsManager.Stop(jobId);
    }

    /// <summary>
    /// Delete a specific job.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId"><see cref="JobId"/> to stop.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/delete/")]
    public async Task DeleteJob(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid jobId)
    {
        var jobsManager = grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(eventStore, @namespace));
        await jobsManager.Delete(jobId);
    }
}
