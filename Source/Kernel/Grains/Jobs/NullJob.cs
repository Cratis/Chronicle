// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents a null <see cref="IJob"/>.
/// </summary>
public class NullJob : IJob
{
    /// <inheritdoc/>
    public Task OnCompleted() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task OnStepFailed(JobStepId stepId, JobStepResult result) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task OnStepStopped(JobStepId stepId, JobStepResult result) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task OnStepSucceeded(JobStepId stepId, JobStepResult result) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Pause() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Resume() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task SetTotalSteps(int totalSteps) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task StatusChanged(JobStatus status, Exception? exception = null) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Stop() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Subscribe(IJobObserver observer) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Unsubscribe(IJobObserver observer) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task WriteState() => Task.CompletedTask;
}
