// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Persistence.Jobs;
using Aksio.DependencyInversion;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Jobs;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
[Route("/api/jobs/{microserviceId}")]
public class Jobs : ControllerBase
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IJobStorage> _jobStorage;
    readonly ProviderFor<IJobStepStorage> _jobStepStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="Jobs"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="jobStorage">Provider for <see cref="IJobStorage"/> for getting job state.</param>
    /// <param name="jobStepStorage">Provider for <see cref="IJobStepStorage"/> for getting job step state.</param>
    public Jobs(
        IExecutionContextManager executionContextManager,
        ProviderFor<IJobStorage> jobStorage,
        ProviderFor<IJobStepStorage> jobStepStorage)
    {
        _executionContextManager = executionContextManager;
        _jobStorage = jobStorage;
        _jobStepStorage = jobStepStorage;
    }

    /// <summary>
    /// Observes all jobs for a specific microservice.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to observe for.</param>
    /// <returns>A <see cref="ClientObservable{T}"/> for observing a collection of <see cref="JobState{T}"/>.</returns>
    [HttpGet]
    public ClientObservable<IEnumerable<JobState<object>>> AllJobs(
        [FromRoute] MicroserviceId microserviceId)
    {
        _executionContextManager.Establish(microserviceId);
        return _jobStorage().ObserveJobs().ToClientObservable();
    }

    /// <summary>
    /// Observes all job steps for a specific job and microservice.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> to observe for.</param>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to observer for.</param>
    /// <returns>A <see cref="ClientObservable{T}"/> for observing a collection of <see cref="JobStepState"/>.</returns>
    [HttpGet("{jobId}/steps")]
    public ClientObservable<IEnumerable<JobStepState>> AllJobSteps(
        [FromRoute] JobId jobId,
        [FromRoute] MicroserviceId microserviceId)
    {
        _executionContextManager.Establish(microserviceId);
        return _jobStepStorage().ObserveForJob(jobId).ToClientObservable();
    }
}
