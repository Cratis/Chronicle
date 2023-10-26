// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents a job that typically runs as long running with <see cref="IJobStep{TRequest}"/>.
/// </summary>
public interface IJob : IGrainWithGuidCompoundKey
{
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
    /// <returns>Awaitable task.</returns>
    Task OnStepSuccessful(JobStepId stepId);

    /// <summary>
    /// Report that a job step has been stopped.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step that was stopped.</param>
    /// <returns>Awaitable task.</returns>
    Task OnStepStopped(JobStepId stepId);

    /// <summary>
    /// Report failure of a job step.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step that failed.</param>
    /// <returns>Awaitable task.</returns>
    Task OnStepFailed(JobStepId stepId);

    /// <summary>
    /// Called when the job has completed.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task OnCompleted();
}

/// <summary>
/// Represents a job that typically runs as long running with <see cref="IJobStep{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request object that gets passed to job.</typeparam>
public interface IJob<TRequest> : IJob
{
    /// <summary>
    /// Start the job.
    /// </summary>
    /// <param name="request">The request object for the job.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(TRequest request);
}
