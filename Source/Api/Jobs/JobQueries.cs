// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Jobs;

namespace Cratis.Chronicle.Api.Jobs;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
/// <param name="jobs"><see cref="IJobs"/> for jobs.</param>
[Route("/api/event-store/{eventStore}/{namespace}/jobs")]
public class JobQueries(IJobs jobs) : ControllerBase
{
    /// <summary>
    /// Observes all jobs for a specific event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <returns>An observable for observing a collection of <see cref="Job"/>.</returns>
    [HttpGet]
    public ISubject<IEnumerable<Job>> AllJobs(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace )
    {
        var subject = new Subject<IEnumerable<Job>>();
        jobs.ObserveJobs(new() { EventStoreName = eventStore, Namespace = @namespace }).Subscribe(subject);
        return subject;
    }
}
