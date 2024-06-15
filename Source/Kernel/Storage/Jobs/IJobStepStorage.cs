// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Jobs;

namespace Cratis.Chronicle.Storage.Jobs;

/// <summary>
/// Defines the storage for a <see cref="JobStepState"/>.
/// </summary>
public interface IJobStepStorage
{
    /// <summary>
    /// Remove the state for all job steps within a specific job.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> the step belongs to.</param>
    /// <returns>Awaitable task.</returns>
    Task RemoveAllForJob(JobId jobId);

    /// <summary>
    /// Remove the state for all non failed job steps within a specific job.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> the step belongs to.</param>
    /// <returns>Awaitable task.</returns>
    Task RemoveAllNonFailedForJob(JobId jobId);

    /// <summary>
    /// Remove the state for a specific job step.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> the step belongs to.</param>
    /// <param name="jobStepId"><see cref="JobStepId"/> for the step.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(JobId jobId, JobStepId jobStepId);

    /// <summary>
    /// Get all job steps for a specific job.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> to get for.</param>
    /// <param name="statuses">Optional collection of <see cref="JobStepStatus"/> to get for. If not specified, all will be returned.</param>
    /// <returns>A collection of job step state objects.</returns>
    Task<IImmutableList<JobStepState>> GetForJob(JobId jobId, params JobStepStatus[] statuses);

    /// <summary>
    /// Count the number of job steps for a specific job for a specific status.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> to count for.</param>
    /// <param name="statuses">Optional collection of <see cref="JobStepStatus"/> to count for. If not specified, all will be counted.</param>
    /// <returns>The number of job steps for the job that holds the specified status.</returns>
    Task<int> CountForJob(JobId jobId, params JobStepStatus[] statuses);

    /// <summary>
    /// Observe job steps for a specific job.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> to observe for.</param>
    /// <returns>An observable of collection of job step state objects.</returns>
    IObservable<IEnumerable<JobStepState>> ObserveForJob(JobId jobId);

    /// <summary>
    /// Read the state for a specific job step.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> the step belongs to.</param>
    /// <param name="jobStepId"><see cref="JobStepId"/> for the step.</param>
    /// <typeparam name="TJobStepState">Concrete type of <see cref="JobStepState"/> to read.</typeparam>
    /// <returns><see cref="JobStepState"/> if it was found, null if not.</returns>
    Task<TJobStepState?> Read<TJobStepState>(JobId jobId, JobStepId jobStepId);

    /// <summary>
    /// Save the state for a specific job step.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> the step belongs to.</param>
    /// <param name="jobStepId"><see cref="JobStepId"/> for the step.</param>
    /// <param name="state">The <see cref="JobStepState"/> to save.</param>
    /// <typeparam name="TJobStepState">Concrete type of <see cref="JobStepState"/> to save.</typeparam>
    /// <returns>Awaitable task.</returns>
    Task Save<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState state);

    /// <summary>
    /// Move a job step being failed.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> the step belongs to.</param>
    /// <param name="jobStepId"><see cref="JobStepId"/> for the step.</param>
    /// <param name="jobStepState">The <see cref="JobStepState"/> to move.</param>
    /// <typeparam name="TJobStepState">Concrete type of <see cref="JobStepState"/> to move.</typeparam>
    /// <returns>Awaitable task.</returns>
    Task MoveToFailed<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState jobStepState);
}
