// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Chronicle.Api.Jobs;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
[Route("/api/event-store/{eventStore}/{namespace}/jobs")]
public class JobCommands : ControllerBase
{
    readonly IJobs _jobs;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobCommands"/> class.
    /// </summary>
    /// <param name="jobs"><see cref="IJobs"/> to work with jobs.</param>
    internal JobCommands(IJobs jobs)
    {
        _jobs = jobs;
    }

    /// <summary>
    /// Stop a specific job.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId">Identifier of the job to resume.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/resume")]
    public Task ResumeJob(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid jobId) =>
        _jobs.Resume(new() { EventStore = eventStore, Namespace = @namespace, JobId = jobId });

    /// <summary>
    /// Stop a specific job.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId">Identifier of the job to stop.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/stop")]
    public Task StopJob(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid jobId) =>
        _jobs.Stop(new() { EventStore = eventStore, Namespace = @namespace, JobId = jobId });

    /// <summary>
    /// Delete a specific job.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId">Identifier of the job to delete.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{jobId}/delete")]
    public Task DeleteJob(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid jobId) =>
        _jobs.Delete(new() { EventStore = eventStore, Namespace = @namespace, JobId = jobId });
}
