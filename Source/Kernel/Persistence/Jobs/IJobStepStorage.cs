// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;

namespace Aksio.Cratis.Kernel.Persistence.Jobs;

/// <summary>
/// Represents the storage for a <see cref="JobStepState"/>.
/// </summary>
/// <typeparam name="TJobStepState">Type of state it is for.</typeparam>
public interface IJobStepStorage<TJobStepState>
{
    /// <summary>
    /// Read the state for a specific job step.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> the step belongs to.</param>
    /// <param name="jobStepId"><see cref="JobStepId"/> for the step.</param>
    /// <returns><see cref="JobStepState"/> if it was found, null if not.</returns>
    Task<TJobStepState?> Read(JobId jobId, JobStepId jobStepId);

    /// <summary>
    /// Save the state for a specific job step.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> the step belongs to.</param>
    /// <param name="jobStepId"><see cref="JobStepId"/> for the step.</param>
    /// <param name="state">The <see cref="JobStepState"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(JobId jobId, JobStepId jobStepId, TJobStepState state);

    /// <summary>
    /// Remove the state for a specific job step.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> the step belongs to.</param>
    /// <param name="jobStepId"><see cref="JobStepId"/> for the step.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(JobId jobId, JobStepId jobStepId);
}
