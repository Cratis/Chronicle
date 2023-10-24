// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Defines a system that manages jobs.
/// </summary>
public interface IJobsManager : IGrainWithIntegerCompoundKey
{
    /// <summary>
    /// Start a job.
    /// </summary>
    /// <param name="jobId">The <see cref="JobId"/> uniquely identifying the job.</param>
    /// <param name="request">The request parameter being passed to the job.</param>
    /// <typeparam name="TJob">Type of job to start.</typeparam>
    /// <typeparam name="TRequest">Type of the request to pass along.</typeparam>
    /// <returns>Awaitable task.</returns>
    Task Start<TJob, TRequest>(JobId jobId, TRequest request)
        where TJob : IJob<TRequest>;

    /// <summary>
    /// Cancel a job if running.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> to cancel.</param>
    /// <returns>Awaitable task.</returns>
    Task Cancel(JobId jobId);

    /// <summary>
    /// Delete an existing job.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(JobId jobId);

    /// <summary>
    /// Report back completion of a job.
    /// </summary>
    /// <param name="jobId">The identifier of the job completed.</param>
    /// <returns>Awaitable task.</returns>
    Task OnCompleted(JobId jobId);

    /// <summary>
    /// Get a collection of all running jobs of specific type.
    /// </summary>
    /// <typeparam name="TJob">Type of job to get for.</typeparam>
    /// <typeparam name="TRequest">Type of request.</typeparam>
    /// <returns>Collection of request instances.</returns>
    Task<IImmutableList<JobState<TRequest>>> GetJobsOfType<TJob, TRequest>()
        where TJob : IJob<TRequest>
        where TRequest : class;
}
