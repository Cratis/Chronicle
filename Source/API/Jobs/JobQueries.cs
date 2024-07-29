// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Jobs;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Jobs"/> class.
/// </remarks>
[Route("/api/events/store/{eventStore}/{namespace}/jobs")]
public class JobQueries() : ControllerBase
{
    /// <summary>
    /// Observes all jobs for a specific event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <returns>An observable for observing a collection of <see cref="JobState"/>.</returns>
    [HttpGet]
    public ISubject<IEnumerable<JobState>> AllJobs(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace) =>
        throw new NotImplementedException();

    /// <summary>
    /// Observes all job steps for a specific job and event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <param name="jobId">Identifier of the job to observe for.</param>
    /// <returns>An observable for observing a collection of <see cref="JobStepState"/>.</returns>
    [HttpGet("{jobId}/steps")]
    public ISubject<IEnumerable<JobStepState>> AllJobSteps(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace,
        [FromRoute] Guid jobId) =>
        throw new NotImplementedException();
}
