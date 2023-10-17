// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents the API for working with jobs.
/// </summary>
[Route("/api/jobs/{microserviceId}")]
public class Jobs : ControllerBase
{
    [HttpGet]
    public ClientObservable<IEnumerable<JobState<object>>> AllJobs(
        [FromRoute] MicroserviceId microserviceId)
    {
        return new ClientObservable<IEnumerable<JobState<object>>>();
    }

    [HttpGet("{jobId}/steps")]
    public ClientObservable<IEnumerable<JobStepState>> AllJobSteps(
        [FromRoute] JobId jobId,
        [FromRoute] MicroserviceId microserviceId)
    {
        return new ClientObservable<IEnumerable<JobStepState>>();
    }
}
