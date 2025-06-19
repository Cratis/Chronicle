// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents a null <see cref="IJob"/>.
/// </summary>
public class NullJob : IJob
{
    /// <inheritdoc/>
    public Task<Result<JobError>> OnStepFailed(JobStepId stepId, JobStepResult jobStepResult) => Task.FromResult(Result.Success<JobError>());

    /// <inheritdoc/>
    public Task<Result<JobError>> OnStepStopped(JobStepId stepId, JobStepResult jobStepResult) => Task.FromResult(Result.Success<JobError>());

    /// <inheritdoc/>
    public Task<Result<JobError>> OnStepSucceeded(JobStepId stepId, JobStepResult result) => Task.FromResult(Result.Success<JobError>());

    /// <inheritdoc/>
    public Task<Result<StopJobError>> Stop() => Task.FromResult(Result.Success<StopJobError>());

    /// <inheritdoc/>
    public Task<Result<ResumeJobSuccess, ResumeJobError>> Resume() => Task.FromResult(Result.Success<ResumeJobSuccess, ResumeJobError>(ResumeJobSuccess.Success));

    /// <inheritdoc/>
    public Task<Result<RemoveJobError>> Remove() => Task.FromResult(Result.Success<RemoveJobError>());
}
