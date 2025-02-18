// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
namespace Cratis.Chronicle.Grains.Jobs;

public class NullJobWithSomeRequest : INullJobWithSomeRequest
{
    public bool Started { get; private set; }
    public Task<Result<StartJobError>> Start(SomeJobRequest request)
    {
        Started = true;
        return Task.FromResult(Result.Success<StartJobError>());
    }

    public Task<Result<PauseJobError>> Pause() => Task.FromResult(Result.Success<PauseJobError>());
    public Task<Result<ResumeJobSuccess, ResumeJobError>> Resume() => Task.FromResult(Result.Success<ResumeJobSuccess, ResumeJobError>(ResumeJobSuccess.Success));
    public Task<Result<JobError>> Stop() => Task.FromResult(Result.Success<JobError>());
    public Task<Result<JobError>> OnStepSucceeded(JobStepId stepId, JobStepResult result) => Task.FromResult(Result.Success<JobError>());
    public Task<Result<JobError>> OnStepStopped(JobStepId stepId, JobStepResult jobStepResult) => Task.FromResult(Result.Success<JobError>());
    public Task<Result<JobError>> OnStepFailed(JobStepId stepId, JobStepResult jobStepResult) => Task.FromResult(Result.Success<JobError>());
    public Task OnCompleted() => Task.CompletedTask;
    public Task<Result<JobError>> WriteStatusChanged(JobStatus status, Exception? exception = null) => Task.FromResult(Result.Success<JobError>());
    public Task<Result<JobError>> SetTotalSteps(int totalSteps) => Task.FromResult(Result.Success<JobError>());
    public Task Subscribe(IJobObserver observer) => Task.CompletedTask;
    public Task Unsubscribe(IJobObserver observer) => Task.CompletedTask;
}
