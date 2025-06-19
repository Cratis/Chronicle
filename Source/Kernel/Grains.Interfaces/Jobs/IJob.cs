// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents a job that typically runs as long-running with <see cref="IJobStep{TRequest, TResult, TState}"/>.
/// </summary>
public interface IJob : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Stop a running job.
    /// <remarks>
    /// Stop is different from Remove in that a stopped Job can be Resumed while a Removed Job cannot.
    /// </remarks>
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task<Result<StopJobError>> Stop();

    /// <summary>
    /// Resume a job, either its paused, stopped or didn't get to finish before the host stopped.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// This method won't do anything if the job is already running.
    /// </remarks>
    Task<Result<ResumeJobSuccess, ResumeJobError>> Resume();

    /// <summary>
    /// Removed a running job.
    /// Remove is different from Stop in that a removed Job cannot be Resumed while a Stopped Job can.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task<Result<RemoveJobError>> Remove();

    /// <summary>
    /// Report a successful completion of a job step.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step that was completed.</param>
    /// <param name="result">The <see cref="JobStepResult"/> for the succeeded step.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobError>> OnStepSucceeded(JobStepId stepId, JobStepResult result);

    /// <summary>
    /// Report that a job step has been stopped.
    /// </summary>
    /// <remarks>
    /// This is different from succeeded and failed in that this signifies that a job step has been manually stopped or paused.
    /// </remarks>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step that was stopped.</param>
    /// <param name="jobStepResult">The <see cref="JobStepResult"/> for the stopped step.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobError>> OnStepStopped(JobStepId stepId, JobStepResult jobStepResult);

    /// <summary>
    /// Report failure of a job step.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step that failed.</param>
    /// <param name="jobStepResult">The <see cref="JobStepResult"/> for the failed step.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobError>> OnStepFailed(JobStepId stepId, JobStepResult jobStepResult);
}

/// <summary>
/// Represents a job that typically runs as long-running with <see cref="IJobStep{TRequest, TResult, TState}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request object that gets passed to job.</typeparam>
public interface IJob<in TRequest> : IJob
    where TRequest : class, IJobRequest
{
    /// <summary>
    /// Start the job.
    /// </summary>
    /// <remarks>
    /// This is different from Resume in that it will prepare job steps before starting them and therefore is a method that cannot be called again after the job steps has been prepared.
    /// </remarks>
    /// <param name="request">The request object for the job.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<StartJobError>> Start(TRequest request);
}
