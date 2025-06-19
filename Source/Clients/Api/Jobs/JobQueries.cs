// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Jobs;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
[Route("/api/event-store/{eventStore}/{namespace}/jobs")]
public class JobQueries : ControllerBase
{
    readonly IJobs _jobs;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobQueries"/> class.
    /// </summary>
    /// <param name="jobs"><see cref="IJobs"/> for jobs.</param>
    internal JobQueries(IJobs jobs)
    {
        _jobs = jobs;
    }

    /// <summary>
    /// Observes all jobs for a specific event store and namespace.
    /// </summary>
    /// <param name="eventStore">Name of the event store the job is for.</param>
    /// <param name="namespace">Namespace within the event store the job is for.</param>
    /// <returns>An observable for observing a collection of <see cref="Job"/>.</returns>
    [HttpGet]
    public ISubject<IEnumerable<Job>> AllJobs(
        [FromRoute] string eventStore,
        [FromRoute] string @namespace) =>
        _jobs.InvokeAndWrapWithTransformSubject(
            token => _jobs.ObserveJobs(new() { EventStore = eventStore, Namespace = @namespace }, token),
            jobs => jobs.ToApi());
}
