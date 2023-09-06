// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents a job that is potentially long running with steps.
/// </summary>
public interface IJob : IGrainWithGuidKey
{
    /// <summary>
    /// Report progress of a job step.
    /// </summary>
    /// <param name="stepId"><see cref="JobStepId"/> to report on.</param>
    /// <param name="progress">The progress to report.</param>
    /// <returns>Awaitable task.</returns>
    Task ReportStepProgress(JobStepId stepId, StepProgress progress);

    /// <summary>
    /// Report completion of a job step.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step that was completed.</param>
    /// <returns>Awaitable task.</returns>
    Task OnStepCompleted(JobStepId stepId);

    /// <summary>
    /// Report failure of a job step.
    /// </summary>
    /// <param name="stepId">The <see cref="JobStepId"/> of the step that failed.</param>
    /// <returns>Awaitable task.</returns>
    Task OnStepFailed(JobStepId stepId);
}
