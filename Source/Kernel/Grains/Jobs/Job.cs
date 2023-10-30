// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Persistence.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJob{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request object that gets passed to job.</typeparam>
/// <typeparam name="TJobState">Type of state for the job.</typeparam>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Jobs)]
public abstract class Job<TRequest, TJobState> : Grain<TJobState>, IJob<TRequest>
    where TJobState : JobState<TRequest>
{
    bool _isRunning;
    JobId _jobId = JobId.NotSet;
    JobKey _jobKey = JobKey.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="Job{TRequest, TJobState}"/> class.
    /// </summary>
    protected Job()
    {
        ThisJob = null!;
    }

    /// <summary>
    /// Gets whether or not to clean up data after the job has completed.
    /// </summary>
    protected virtual bool RemoveAfterCompleted => false;

    /// <summary>
    /// Gets the job as a Grain reference.
    /// </summary>
    protected IJob<TRequest> ThisJob { get; private set; }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        StatusChanged(JobStatus.Running);

        _jobId = this.GetPrimaryKey(out var keyExtension);
        _jobKey = (JobKey)keyExtension;

        ThisJob = GrainFactory.GetGrain(GrainReference.GrainId).AsReference<IJob<TRequest>>();

        var type = GetType();
        var grainType = type.GetInterfaces().SingleOrDefault(_ => _.Name == $"I{type.Name}") ?? throw new InvalidGrainNameForJob(type);
        State.Name = GetType().Name;
        State.Type = grainType;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task Start(TRequest request)
    {
        _isRunning = true;
        State.Request = request!;
        return PrepareSteps(request);
    }

    /// <inheritdoc/>
    public async Task Resume()
    {
        if (_isRunning) return;

        var executionContextManager = ServiceProvider.GetRequiredService<IExecutionContextManager>();
        executionContextManager.Establish(_jobKey.TenantId, executionContextManager.Current.CorrelationId, _jobKey.MicroserviceId);

        var stepStorage = ServiceProvider.GetRequiredService<IJobStepStorage>();
        var steps = await stepStorage.GetForJob(_jobId, JobStepStatus.Scheduled, JobStepStatus.Running);
        foreach (var step in steps)
        {
            var jobStep = (GrainFactory.GetGrain(step.GrainId) as IJobStep)!;
            await jobStep.Resume(this.GetGrainId());
        }
    }

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

        await HandleCompletion();
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task OnStepStopped(JobStepId stepId) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task OnStepFailed(JobStepId stepId)
    {
        State.Progress.FailedSteps++;

        await HandleCompletion();
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task AddStep<TJobStep, TJobStepRequest>(TJobStepRequest request)
        where TJobStep : IJobStep<TJobStepRequest>
    {
        var jobStepId = JobStepId.New();
        var jobId = this.GetPrimaryKey(out var keyExtension);
        var jobKey = (JobKey)keyExtension!;
        var jobStep = GrainFactory.GetGrain<TJobStep>(jobStepId, keyExtension: new JobStepKey(jobId, jobKey.MicroserviceId, jobKey.TenantId));
        await jobStep.Start(this.GetGrainId(), request);
        State.Progress.TotalSteps++;
    }

    /// <summary>
    /// Start the job.
    /// </summary>
    /// <param name="request">Request to start with.</param>
    /// <returns>Awaitable task.</returns>
    protected abstract Task PrepareSteps(TRequest request);

    async Task HandleCompletion()
    {
        if (State.Progress.IsCompleted)
        {
            var id = this.GetPrimaryKey(out var keyExtension);
            var key = (JobKey)keyExtension!;
            await GrainFactory
                    .GetGrain<IJobsManager>(0, new JobsManagerKey(key.MicroserviceId, key.TenantId))
                    .OnCompleted(id, State.Status);
            await OnCompleted();

            if (State.Progress.FailedSteps > 0)
            {
                StatusChanged(JobStatus.CompletedWithFailures);
            }
            else
            {
                StatusChanged(JobStatus.CompletedSuccessfully);
            }

            State.Remove = RemoveAfterCompleted;
        }
    }

    void StatusChanged(JobStatus status)
    {
        State.StatusChanges.Add(new JobStatusChanged
        {
            Status = status,
            Occurred = DateTimeOffset.UtcNow
        });
    }
}
