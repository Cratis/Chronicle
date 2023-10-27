// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime;
using Orleans.SyncWork;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents a step in a job.
/// </summary>
public interface IJobStep : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Resume a job step.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Resume();

    /// <summary>
    /// Stop the job step.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Stop();

    /// <summary>
    /// Report a status change.
    /// </summary>
    /// <param name="status">The <see cref="JobStepStatus"/> to change to.</param>
    /// <returns>Awaitable task.</returns>
    Task ReportStatusChange(JobStepStatus status);

    /// <summary>
    /// Report the step has failed.
    /// </summary>
    /// <param name="exceptionMessages">Collection of exception messages.</param>
    /// <param name="exceptionStackTrace">Exception stack trace.</param>
    /// <returns>Awaitable task.</returns>
    Task ReportFailure(IList<string> exceptionMessages, string exceptionStackTrace);
}

/// <summary>
/// Represents a step in a job.
/// </summary>
/// <typeparam name="TRequest">Type of the request for the job.</typeparam>
public interface IJobStep<TRequest> : ISyncWorker<TRequest, object>, IJobStep
{
    /// <summary>
    /// Start the job step.
    /// </summary>
    /// <param name="jobId">The <see cref="GrainId"/> for the parent job.</param>
    /// <param name="request">Request to start it with.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(GrainId jobId, TRequest request);
}
