// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents a null <see cref="IJob"/>.
/// </summary>
public class NullJob : IJob
{
    /// <inheritdoc/>
    public Task OnStepFailed(JobStepId stepId) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task OnStepStopped(JobStepId stepId) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task OnStepSuccessful(JobStepId stepId) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ReportStepProgress(JobStepId stepId, JobStepProgress progress) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Stop() => Task.CompletedTask;
}
