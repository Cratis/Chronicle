// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents a null <see cref="IJob"/>.
/// </summary>
public class NullJob : IJob
{
    /// <inheritdoc/>
    public Task OnCompleted() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<Result<JobError>> OnStepFailed(JobStepId stepId, JobStepResult jobStepResult) => Task.FromResult(Result.Success<JobError>());

    /// <inheritdoc/>
    public Task<Result<JobError>> OnStepStopped(JobStepId stepId, JobStepResult jobStepResult) => Task.FromResult(Result.Success<JobError>());

    /// <inheritdoc/>
    public Task<Result<JobError>> OnStepSucceeded(JobStepId stepId, JobStepResult result) => Task.FromResult(Result.Success<JobError>());

    /// <inheritdoc/>
    public Task<Result<JobError>> Pause() => Task.FromResult(Result.Success<JobError>());

    /// <inheritdoc/>
    public Task<Result<ResumeJobSuccess, ResumeJobError>> Resume() => Task.FromResult(Result.Success<ResumeJobSuccess, ResumeJobError>(ResumeJobSuccess.Success));

    /// <inheritdoc/>
    public Task<Result<JobError>> SetTotalSteps(int totalSteps) => Task.FromResult(Result.Success<JobError>());

    /// <inheritdoc/>
    public Task<Result<JobError>> WriteStatusChanged(JobStatus status, Exception? exception = null) => Task.FromResult(Result.Success<JobError>());

    /// <inheritdoc/>
    public Task<Result<JobError>> Stop() => Task.FromResult(Result.Success<JobError>());

    /// <inheritdoc/>
    public Task Subscribe(IJobObserver observer) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task Unsubscribe(IJobObserver observer) => Task.CompletedTask;
}
