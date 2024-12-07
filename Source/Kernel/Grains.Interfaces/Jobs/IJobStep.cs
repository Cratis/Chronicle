// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Workers;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents a step in a job.
/// </summary>
public interface IJobStep : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Start the job step.
    /// </summary>
    /// <param name="jobId">The <see cref="GrainId"/> for the parent job.</param>
    /// <param name="request">Request to start it with.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobStepPrepareStartError>> Start(GrainId jobId, object request);

    /// <summary>
    /// Prepare the job step.
    /// </summary>
    /// <param name="request">Request to prepare it with.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobStepPrepareStartError>> Prepare(object request);

    /// <summary>
    /// Pause the job step.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task<Result<JobStepError>> Pause();

    /// <summary>
    /// Resume a job step.
    /// </summary>
    /// <param name="jobId">The <see cref="GrainId"/> for the parent job.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobStepResumeSuccess, JobStepError>> Resume(GrainId jobId);

    /// <summary>
    /// Stop the job step.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task<Result<JobStepError>> Stop();

    /// <summary>
    /// Report a status change.
    /// </summary>
    /// <param name="status">The <see cref="JobStepStatus"/> to change to.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobStepError>> ReportStatusChange(JobStepStatus status);

    /// <summary>
    /// Report the step has failed.
    /// </summary>
    /// <param name="error">The <see cref="PerformJobStepError"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobStepError>> ReportFailure(PerformJobStepError error);
}

/// <summary>
/// Represents a step in a job.
/// </summary>
/// <typeparam name="TRequest">Type of the request for the job step.</typeparam>
/// <typeparam name="TResult">Type of the result for the job step.</typeparam>
public interface IJobStep<in TRequest, TResult> : ICpuBoundWorker<TRequest, JobStepResult>, IJobStep
{
    /// <summary>
    /// Start the job step.
    /// </summary>
    /// <param name="jobId">The <see cref="GrainId"/> for the parent job.</param>
    /// <param name="request">Request to start it with.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobStepPrepareStartError>> Start(GrainId jobId, TRequest request);

    /// <summary>
    /// Prepare the job step.
    /// </summary>
    /// <param name="request">Request to prepare it with.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobStepPrepareStartError>> Prepare(TRequest request);
}
