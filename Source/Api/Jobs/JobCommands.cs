// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Chronicle.Api.Jobs;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
/// <param name="jobs"><see cref="IJobs"/> to work with jobs.</param>
[Route("/api/event-store/{eventStore}/{namespace}/jobs")]
public class JobCommands(IJobs jobs) : ControllerBase
{
    /// <summary>
    /// Stop a specific job.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId">Identifier of the job to resume.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/resume/")]
    public Task ResumeJob(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid jobId) =>
        jobs.Resume(new() { EventStoreName = eventStore, Namespace = @namespace, JobId = jobId });

    /// <summary>
    /// Stop a specific job.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId">Identifier of the job to stop.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/stop/")]
    public Task StopJob(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid jobId) =>
        jobs.Stop(new() { EventStoreName = eventStore, Namespace = @namespace, JobId = jobId });

    /// <summary>
    /// Delete a specific job.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId">Identifier of the job to delete.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/delete/")]
    public Task DeleteJob(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid jobId) =>
        jobs.Delete(new() { EventStoreName = eventStore, Namespace = @namespace, JobId = jobId });
}
