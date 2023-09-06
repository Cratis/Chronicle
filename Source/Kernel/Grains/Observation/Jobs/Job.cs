// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJob"/>.
/// </summary>
public class Job : IJob
{
    /// <inheritdoc/>
    public Task ReportStepProgress(JobStepId stepId, StepProgress progress) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task OnStepCompleted(JobStepId stepId) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task OnStepFailed(JobStepId stepId) => throw new NotImplementedException();

    /// <summary>
    /// Start the job.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task Start() => Task.CompletedTask;
}
