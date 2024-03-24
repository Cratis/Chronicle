// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Jobs;
using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.Jobs;
using Cratis.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Jobs.Queries;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Jobs"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
[Route("/api/events/store/{eventStore}/{namespace}/jobs")]
public class Jobs(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Observes all jobs for a specific event store and namespace.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> to observe for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to observe for. </param>
    /// <returns>A <see cref="ClientObservable{T}"/> for observing a collection of <see cref="JobState"/>.</returns>
    [HttpGet]
    public ClientObservable<IEnumerable<JobState>> AllJobs(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace) =>
        storage.GetEventStore(eventStore).GetNamespace(@namespace).Jobs.ObserveJobs().ToClientObservable();

    /// <summary>
    /// Observes all job steps for a specific job and event store and namespace.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> to observe for.</param>
    /// <param name="eventStore"><see cref="EventStoreName"/> to observe for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to observe for. </param>
    /// <returns>A <see cref="ClientObservable{T}"/> for observing a collection of <see cref="JobStepState"/>.</returns>
    [HttpGet("{jobId}/steps")]
    public ClientObservable<IEnumerable<JobStepState>> AllJobSteps(
        [FromRoute] JobId jobId,
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace) =>
        storage.GetEventStore(eventStore).GetNamespace(@namespace).JobSteps.ObserveForJob(jobId).ToClientObservable();
}
