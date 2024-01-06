// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Jobs;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/jobs")]
public class Jobs : ControllerBase
{
    readonly IClusterStorage _clusterStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="Jobs"/> class.
    /// </summary>
    /// <param name="clusterStorage"><see cref="IClusterStorage"/> for accessing underlying storage.</param>
    public Jobs(IClusterStorage clusterStorage)
    {
        _clusterStorage = clusterStorage;
    }

    /// <summary>
    /// Observes all jobs for a specific microservice.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to observe for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> to observe for. </param>
    /// <returns>A <see cref="ClientObservable{T}"/> for observing a collection of <see cref="JobState"/>.</returns>
    [HttpGet]
    public ClientObservable<IEnumerable<JobState>> AllJobs(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId) =>
        _clusterStorage.GetEventStore((string)microserviceId).GetNamespace(tenantId).Jobs.ObserveJobs().ToClientObservable();

    /// <summary>
    /// Observes all job steps for a specific job and microservice.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> to observe for.</param>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to observe for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> to observe for. </param>
    /// <returns>A <see cref="ClientObservable{T}"/> for observing a collection of <see cref="JobStepState"/>.</returns>
    [HttpGet("{jobId}/steps")]
    public ClientObservable<IEnumerable<JobStepState>> AllJobSteps(
        [FromRoute] JobId jobId,
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId) =>
        _clusterStorage.GetEventStore((string)microserviceId).GetNamespace(tenantId).JobSteps.ObserveForJob(jobId).ToClientObservable();
}
