// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Providers;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJob{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request object that gets passed to job.</typeparam>
/// <typeparam name="TJobState">Type of state for the job.</typeparam>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Jobs)]
public abstract class Job<TRequest, TJobState> : Grain<TJobState>, IJob<TRequest>
    where TJobState : JobState
{
    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        StatusChanged(JobStatus.Started);
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public abstract Task Start(TRequest request);

    /// <inheritdoc/>
    public async Task Stop()
    {
        StatusChanged(JobStatus.Stopped);
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public virtual Task OnCompleted() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task OnStepSuccessful(JobStepId stepId)
    {
        State.Progress.SuccessfulSteps++;

        await WriteStateAsync();
        await HandleCompletion();
    }

    /// <inheritdoc/>
    public Task OnStepStopped(JobStepId stepId) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task OnStepFailed(JobStepId stepId)
    {
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
        await jobStep.Start(this.GetGrainId(), request);
        State.Progress.TotalSteps++;
    }

    async Task HandleCompletion()
    {
        if (State.Progress.IsCompleted)
        {
            var id = this.GetPrimaryKey(out var keyExtension);
            var key = (JobKey)keyExtension!;
            await GrainFactory.GetGrain<IJobsManager>(0).OnCompleted(key.MicroserviceId, key.TenantId, id);
            await OnCompleted();

            if (State.Progress.FailedSteps > 0)
            {
                StatusChanged(JobStatus.CompletedWithFailures);
            }
            else
            {
                StatusChanged(JobStatus.Completed);
            }
        }
    }

    void StatusChanged(JobStatus status)
    {
        State.StatusChanges.Add(new JobStatusChanged
        {
            Status = status,
            Occurred = DateTimeOffset.UtcNow
        });
        State.Status = status;
    }
}
