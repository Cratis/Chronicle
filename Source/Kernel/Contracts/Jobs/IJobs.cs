// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Primitives;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts.Jobs;

/// <summary>
/// Defines the contract work working with the jobs system.
/// </summary>
[Service]
public interface IJobs
{
    /// <summary>
    /// Stop a specific job.
    /// </summary>
    /// <param name="command"><see cref="StopJob"/> command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Stop(StopJob command, CallContext context = default);

    /// <summary>
    /// Resume a specific job.
    /// </summary>
    /// <param name="command"><see cref="StopJob"/> command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Resume(ResumeJob command, CallContext context = default);

    /// <summary>
    /// Delete a specific job.
    /// </summary>
    /// <param name="command"><see cref="StopJob"/> command.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Delete(DeleteJob command, CallContext context = default);

    /// <summary>
    /// Get a specific job.
    /// </summary>
    /// <param name="request"><see cref="GetJobRequest"/> representing what to get.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Either <see cref="Job"/> instance or a <see cref="JobError"/> .</returns>
    [Operation]
    Task<OneOf<Job, JobError>> GetJob(GetJobRequest request, CallContext context = default);

    /// <summary>
    /// Get all jobs.
    /// </summary>
    /// <param name="request">The <see cref="GetJobsRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Collection of all jobs.</returns>
    [Operation]
    Task<IEnumerable<Job>> GetJobs(GetJobsRequest request, CallContext context = default);

    /// <summary>
    /// Observe all jobs.
    /// </summary>
    /// <param name="request">The <see cref="GetJobsRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Observable of collection of all jobs.</returns>
    [Operation]
    IObservable<IEnumerable<Job>> ObserveJobs(GetJobsRequest request, CallContext context = default);

    /// <summary>
    /// Gets the job steps for a specific job.
    /// </summary>
    /// <param name="request"><see cref="GetJobStepsRequest"/> representing what to get.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Collection of <see cref="JobStep"/>.</returns>
    [Operation]
    Task<IEnumerable<JobStep>> GetJobSteps(GetJobStepsRequest request, CallContext context = default);
}
