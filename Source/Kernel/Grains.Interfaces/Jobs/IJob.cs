// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents a job that typically runs as long-running with <see cref="IJobStep{TRequest, TResult}"/>.
/// </summary>
public interface IJob : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Pause a running job.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobError>> Pause();

    /// <summary>
    /// Resume a job, either its paused, stopped or didn't get to finish before the host stopped.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// This method won't do anything if the job is already running.
    /// </remarks>
    Task<Result<ResumeJobSuccess, ResumeJobError>> Resume();

    /// <summary>
    /// Stop a running job.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobError>> Stop();

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

    /// <summary>
    /// Called when the job has completed.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task OnCompleted();

    /// <summary>
    /// Adds a status change to the job.
    /// </summary>
    /// <param name="status"><see cref="JobStatus"/> to add.</param>
    /// <param name="exception">Optional <see cref="Exception"/> associated with the status.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// This is called internally. Do not call this from external code.
    /// Due to the intricacy of how jobs run from different task contexts outside of Orleans, this is needed to be able to
    /// update state.
    /// </remarks>
    Task<Result<JobError>> WriteStatusChanged(JobStatus status, Exception? exception = null);

    /// <summary>
    /// Set the total number of steps for the job.
    /// </summary>
    /// <param name="totalSteps">The total of steps for the job.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// This is called internally. Do not call this from external code.
    /// Due to the intricacy of how jobs run from different task contexts outside of Orleans, this is needed to be able to
    /// update state.
    /// </remarks>
    Task<Result<JobError>> SetTotalSteps(int totalSteps);

    /// <summary>
    /// Subscribe to job events.
    /// </summary>
    /// <param name="observer"><see cref="IJobObserver"/> that will be notified.</param>
    /// <returns>Awaitable task.</returns>
    Task Subscribe(IJobObserver observer);

    /// <summary>
    /// Unsubscribe to job events.
    /// </summary>
    /// <param name="observer"><see cref="IJobObserver"/> that will be notified.</param>
    /// <returns>Awaitable task.</returns>
    Task Unsubscribe(IJobObserver observer);
}

/// <summary>
/// Represents a job that typically runs as long-running with <see cref="IJobStep{TRequest, TResult}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request object that gets passed to job.</typeparam>
public interface IJob<in TRequest> : IJob
    where TRequest : class, IJobRequest
{
    /// <summary>
    /// Start the job.
    /// </summary>
    /// <param name="request">The request object for the job.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobError>> Start(TRequest request);
}