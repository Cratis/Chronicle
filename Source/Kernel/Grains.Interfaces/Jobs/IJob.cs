// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Jobs;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents a job that typically runs as long running with <see cref="IJobStep{TRequest, TResult}"/>.
/// </summary>
public interface IJob : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Pause a running job.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Pause();

    /// <summary>
    /// Resume a job, either its paused, stopped or didn't get to finish before the host stopped.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// This method won't do anything if the job is already running.
    /// </remarks>
    Task Resume();

    /// <summary>
    /// Stop a running job.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Stop();

    /// <summary>
    /// Report a successful completion of a job step.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step that was completed.</param>
    /// <param name="result">The <see cref="JobStepResult"/> for the succeeded step.</param>
    /// <returns>Awaitable task.</returns>
    Task OnStepSucceeded(JobStepId stepId, JobStepResult result);

    /// <summary>
    /// Report that a job step has been stopped.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step that was stopped.</param>
    /// <param name="result">The <see cref="JobStepResult"/> for the stopped step.</param>
    /// <returns>Awaitable task.</returns>
    Task OnStepStopped(JobStepId stepId, JobStepResult result);

    /// <summary>
    /// Report failure of a job step.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step that failed.</param>
    /// <param name="result">The <see cref="JobStepResult"/> for the failed step.</param>
    /// <returns>Awaitable task.</returns>
    Task OnStepFailed(JobStepId stepId, JobStepResult result);

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
    Task StatusChanged(JobStatus status, Exception? exception = null);

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
    Task SetTotalSteps(int totalSteps);

    /// <summary>
    /// Write state to storage.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task WriteState();

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
/// Represents a job that typically runs as long running with <see cref="IJobStep{TRequest, TResult}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request object that gets passed to job.</typeparam>
public interface IJob<TRequest> : IJob
    where TRequest : class
{
    /// <summary>
    /// Start the job.
    /// </summary>
    /// <param name="request">The request object for the job.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(TRequest request);
}
