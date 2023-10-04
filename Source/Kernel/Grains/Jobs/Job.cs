// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJob{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request object that gets passed to job.</typeparam>
public abstract class Job<TRequest> : Grain<JobState>, IJob<TRequest>
{
    /// <inheritdoc/>
    public abstract Task Start(TRequest request);

    /// <inheritdoc/>
    public Task Stop() => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task ReportStepProgress(JobStepId stepId, JobStepProgress progress) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task OnStepSuccessful(JobStepId stepId)
    {
        State.Steps.Remove(stepId);
        State.Progress.SuccessfulSteps++;

        await WriteStateAsync();
        await HandleCompletion();
    }

    /// <inheritdoc/>
    public Task OnStepStopped(JobStepId stepId) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task OnStepFailed(JobStepId stepId)
    {
        var step = State.Steps[stepId];
        State.Steps.Remove(stepId);
        State.FailedSteps[stepId] = step;
        State.Progress.FailedSteps++;

        await WriteStateAsync();
        await HandleCompletion();
    }

    /// <summary>
    /// Add a step to the job.
    /// </summary>
    /// <param name="request">Request for the step.</param>
    /// <typeparam name="TJobStep">Type of job step.</typeparam>
    /// <typeparam name="TJobStepRequest">Type of request for the step.</typeparam>
    /// <returns>Awaitable task.</returns>
    protected async Task AddStep<TJobStep, TJobStepRequest>(TJobStepRequest request)
        where TJobStep : IJobStep<TJobStepRequest>
    {
        var jobStepId = JobStepId.New();
        var jobStep = GrainFactory.GetGrain<TJobStep>(jobStepId);
        await jobStep.Start(request);
        State.Steps[jobStepId] = new JobStepState
        {
            Status = JobStepStatus.Running
        };
    }

    async Task HandleCompletion()
    {
        if (State.Progress.IsCompleted)
        {
            await GrainFactory.GetGrain<IJobsManager>(0).OnCompleted(this.GetPrimaryKey());
        }
    }
}
