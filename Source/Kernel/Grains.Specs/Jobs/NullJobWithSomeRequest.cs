// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
namespace Cratis.Chronicle.Grains.Jobs;

public class NullJobWithSomeRequest : INullJobWithSomeRequest
{
    public bool Started { get; private set; }
    public Task Start(SomeJobRequest request)
    {
        Started = true;
        return Task.CompletedTask;
    }

    public Task Pause() => Task.CompletedTask;
    public Task Resume() => Task.CompletedTask;
    public Task Stop() => Task.CompletedTask;
    public Task OnStepSucceeded(JobStepId stepId, JobStepResult result) => Task.CompletedTask;
    public Task OnStepStopped(JobStepId stepId, JobStepResult result) => Task.CompletedTask;
    public Task OnStepFailed(JobStepId stepId, JobStepResult result) => Task.CompletedTask;
    public Task OnCompleted() => Task.CompletedTask;
    public Task StatusChanged(JobStatus status, Exception? exception = null) => Task.CompletedTask;
    public Task SetTotalSteps(int totalSteps) => Task.CompletedTask;
    public Task WriteState() => Task.CompletedTask;
    public Task Subscribe(IJobObserver observer) => Task.CompletedTask;
    public Task Unsubscribe(IJobObserver observer) => Task.CompletedTask;
}
