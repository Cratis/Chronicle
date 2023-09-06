// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Defines a system that manages jobs.
/// </summary>
public interface IJobsManager : IGrainWithIntegerKey
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
        where TJob : IJob;

    /// <summary>
    /// Report progress of a job.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> to report for.</param>
    /// <param name="progress">The <see cref="JobProgress"/> for the job.</param>
    /// <returns>Awaitable task.</returns>
    Task ReportProgress(JobId jobId, JobProgress progress);

    /// <summary>
    /// Report back completion of a job.
    /// </summary>
    /// <param name="jobId">The identifier of the job completed.</param>
    /// <returns>Awaitable task.</returns>
    Task OnCompleted(JobId jobId);
}
