// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Workers;
using Cratis.Monads;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Represents a step in a job.
/// </summary>
public interface IJobStep : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Prepare the job step.
    /// </summary>
    /// <param name="request">Request to prepare it with.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<PrepareJobStepError>> Prepare(object request);

    /// <summary>
    /// Start the job step.
    /// </summary>
    /// <param name="jobGrainId">The <see cref="GrainId"/> for the parent job.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<StartJobStepError>> Start(GrainId jobGrainId);

    /// <summary>
    /// Stop the job step.
    /// </summary>
    /// <param name="removing">Whether job step is being removed.</param>
    /// <remarks>
    /// A stopped job step can be started again later given it has been prepared.
    /// </remarks>
    /// <returns>Awaitable task.</returns>
    Task<Result<JobStepError>> Stop(bool removing);

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
/// <typeparam name="TState">Type of the state.</typeparam>
public interface IJobStep<in TRequest, TResult, TState> : IGrainWithBackgroundTask<TRequest, JobStepResult>, IJobStep
{
    /// <summary>
    /// Prepare the job step.
    /// </summary>
    /// <param name="request">Request to prepare it with.</param>
    /// <returns>Awaitable task.</returns>
    Task<Result<PrepareJobStepError>> Prepare(TRequest request);

    /// <summary>
    /// Gets the <typeparamref name="TState"/>.
    /// </summary>
    /// <returns>The state.</returns>
    [AlwaysInterleave]
    Task<TState> GetState();
}
