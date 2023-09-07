// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJob"/>.
/// </summary>
public class Job : Grain<JobState>, IJob
{
    /// <inheritdoc/>
    public Task ReportStepProgress(JobStepId stepId, JobStepProgress progress) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task OnStepCompleted(JobStepId stepId)
    {
        State.Progress.CompletedSteps++;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task OnStepFailed(JobStepId stepId)
    {
        State.Progress.FailedSteps++;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public virtual Task Stop() => Task.CompletedTask;

    /// <summary>
    /// Start the job.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task Start() => Task.CompletedTask;
}
