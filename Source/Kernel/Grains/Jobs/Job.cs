// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Aksio.Cratis.Kernel.Orleans.Observers;
using Aksio.Cratis.Kernel.Persistence.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Providers;
using Orleans.Runtime;

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
    IDictionary<JobStepId, JobStepGrainAndRequest> _jobStepGrains = new Dictionary<JobStepId, JobStepGrainAndRequest>();
    ObserverManager<IJobObserver>? _observers;
    bool _isRunning;
    JobId _jobId = JobId.NotSet;
    JobKey _jobKey = JobKey.NotSet;
    IDisposable? _subscriptionTimer;

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
        _observers = new(
            TimeSpan.FromMinutes(1),
            ServiceProvider.GetService<ILogger<ObserverManager<IJobObserver>>>() ?? new NullLogger<ObserverManager<IJobObserver>>(),
            "JobObservers");

        await StatusChanged(JobStatus.Preparing);

        _jobId = this.GetPrimaryKey(out var keyExtension);
        _jobKey = (JobKey)keyExtension;

        ThisJob = GrainFactory.GetGrain(GrainReference.GrainId).AsReference<IJob<TRequest>>();

        var type = GetType();
        var grainType = type.GetInterfaces().SingleOrDefault(_ => _.Name == $"I{type.Name}") ?? throw new InvalidGrainName(type);
        State.Name = GetType().Name;
        State.Type = grainType;
        await WriteStateAsync();
    }

    /// <inheritdoc/>
    public async Task Start(TRequest request)
    {
        _isRunning = true;
        State.Details = GetJobDetails(request);
        State.Request = request!;
        await WriteStateAsync();

        var grainId = this.GetGrainId();
        var tcs = new TaskCompletionSource<IImmutableList<JobStepDetails>>();

        PrepareAllSteps(request, tcs);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        tcs.Task.ContinueWith(async (Task<IImmutableList<JobStepDetails>> jobStepsTask) =>
        {
            var jobSteps = await jobStepsTask;
            if (jobSteps.Count == 0)
            {
                State.Remove = true;
                await OnCompleted();
                await StatusChanged(JobStatus.CompletedSuccessfully);
                await WriteStateAsync();
                return;
            }
            _jobStepGrains = CreateGrainsFromJobSteps(jobSteps);
            await StatusChanged(JobStatus.PreparingSteps);
            await WriteStateAsync();

            await SubscribeJobEventsForAllJobSteps();
            PrepareAndStartAllJobSteps(grainId);
        });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    /// <inheritdoc/>
    public async Task Resume()
    {
        if (_isRunning) return;

        var executionContextManager = ServiceProvider.GetRequiredService<IExecutionContextManager>();
        executionContextManager.Establish(_jobKey.TenantId, executionContextManager.Current.CorrelationId, _jobKey.MicroserviceId);

        if (!await CanResume())
        {
            await StatusChanged(JobStatus.Paused);
            await WriteStateAsync();
            return;
        }

        var stepStorage = ServiceProvider.GetRequiredService<IJobStepStorage>();
        var steps = await stepStorage.GetForJob(_jobId, JobStepStatus.Scheduled, JobStepStatus.Running, JobStepStatus.Paused);
        foreach (var step in steps)
        {
            var jobStep = (GrainFactory.GetGrain((Type)step.Type, step.Id.JobStepId, keyExtension: new JobStepKey(_jobId, _jobKey.MicroserviceId, _jobKey.TenantId)) as IJobStep)!;
            await jobStep.Resume(this.GetGrainId());
        }
    }

    /// <inheritdoc/>
    public async Task Pause()
    {
        if (State.Status == JobStatus.Stopped || State.Status == JobStatus.CompletedSuccessfully || State.Status == JobStatus.CompletedWithFailures)
        {
            return;
        }

        _observers?.Notify(_ => _.OnJobPaused());

        await OnCompleted();

        await StatusChanged(JobStatus.Paused);
        await WriteStateAsync();

        _subscriptionTimer?.Dispose();
    }

    /// <inheritdoc/>
    public async Task Stop()
    {
        if (State.Status == JobStatus.Stopped || State.Status == JobStatus.CompletedSuccessfully || State.Status == JobStatus.CompletedWithFailures)
        {
            return;
        }

        _observers?.Notify(_ => _.OnJobStopped());

        var stepStorage = ServiceProvider.GetRequiredService<IJobStepStorage>();
        await stepStorage.RemoveAllForJob(_jobId);
        await OnCompleted();

        await StatusChanged(JobStatus.Stopped);
        await WriteStateAsync();

        _subscriptionTimer?.Dispose();
    }

    /// <inheritdoc/>
    public virtual Task OnCompleted() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task OnStepSucceeded(JobStepId stepId)
    {
        State.Progress.SuccessfulSteps++;

        await HandleCompletion();
        await WriteStateAsync();

        await Unsubscribe(_jobStepGrains[stepId].Grain.AsReference<IJobObserver>());
        _jobStepGrains.Remove(stepId);
    }

    /// <inheritdoc/>
    public Task OnStepStopped(JobStepId stepId) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task OnStepFailed(JobStepId stepId)
    {
        State.Progress.FailedSteps++;

        await HandleCompletion();
        await WriteStateAsync();

        await Unsubscribe(_jobStepGrains[stepId].Grain.AsReference<IJobObserver>());
        _jobStepGrains.Remove(stepId);
    }

    /// <inheritdoc/>
    public Task SetTotalSteps(int totalSteps)
    {
        State.Progress.TotalSteps = totalSteps;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StatusChanged(JobStatus status)
    {
        State.StatusChanges.Add(new JobStatusChanged
        {
            Status = status,
            Occurred = DateTimeOffset.UtcNow
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task WriteState() => WriteStateAsync();

    /// <inheritdoc/>
    public Task Subscribe(IJobObserver observer)
    {
        _observers?.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Unsubscribe(IJobObserver observer)
    {
        _observers?.Unsubscribe(observer);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Create a new <see cref="IJobStep"/>.
    /// </summary>
    /// <param name="request">The request associated with the step.</param>
    /// <typeparam name="TJobStep">The type of job step to create.</typeparam>
    /// <returns>A new instance of the job step.</returns>
    protected JobStepDetails CreateStep<TJobStep>(object request)
        where TJobStep : IJobStep
    {
        var jobStepId = JobStepId.New();
        var jobId = this.GetPrimaryKey(out var keyExtension);
        var jobKey = (JobKey)keyExtension!;
        return new(
            typeof(TJobStep),
            jobStepId,
            new JobStepKey(jobId, jobKey.MicroserviceId, jobKey.TenantId),
            request);
    }

    /// <summary>
    /// Start the job.
    /// </summary>
    /// <param name="request">Request to start with.</param>
    /// <returns>Awaitable task.</returns>
    protected abstract Task<IImmutableList<JobStepDetails>> PrepareSteps(TRequest request);

    /// <summary>
    /// Check if the job can be resumed.
    /// </summary>
    /// <returns>True if it can, false if not.</returns>
    protected virtual Task<bool> CanResume() => Task.FromResult(true);

    /// <summary>
    /// Get the details for the job. This is for display purposes.
    /// </summary>
    /// <param name="request">The job request.</param>
    /// <returns>The <see cref="JobDetails"/>.</returns>
    protected virtual JobDetails GetJobDetails(TRequest request) => JobDetails.NotSet;

    IDictionary<JobStepId, JobStepGrainAndRequest> CreateGrainsFromJobSteps(IImmutableList<JobStepDetails> jobSteps) =>
        jobSteps.ToDictionary(
            _ => _.Id,
            _ => new JobStepGrainAndRequest(
                (GrainFactory.GetGrain(_.Type, _.Id, keyExtension: _.Key) as IJobStep)!,
                _.Request));
    void PrepareAllSteps(TRequest request, TaskCompletionSource<IImmutableList<JobStepDetails>> tcs) => _ = Task.Run(async () =>
    {
        var steps = await PrepareSteps(request);
        await ThisJob.SetTotalSteps(steps.Count);
        await ThisJob.WriteState();
        tcs.SetResult(steps);
    });

    void PrepareAndStartAllJobSteps(GrainId grainId) => _ = Task.Run(async () =>
    {
        await PrepareJobStepsForRunning();

        await ThisJob.StatusChanged(JobStatus.Running);
        await ThisJob.WriteState();

        await StartAllJobSteps(grainId);
    });

    async Task StartAllJobSteps(GrainId grainId)
    {
        foreach (var jobStep in _jobStepGrains.Values)
        {
            var task = JobStepInvoker.StartMethod.GetGenericMethodDefinition()
                .MakeGenericMethod(jobStep.Request.GetType())
                .Invoke(null, new object[] { jobStep.Grain, grainId, jobStep.Request }) as Task;
            await task!;
        }
    }

    async Task PrepareJobStepsForRunning()
    {
        foreach (var jobStep in _jobStepGrains.Values)
        {
            var task = JobStepInvoker.PrepareMethod.GetGenericMethodDefinition()
                .MakeGenericMethod(jobStep.Request.GetType())
                .Invoke(null, new object[] { jobStep.Grain, jobStep.Request }) as Task;

            await task!;
        }
    }

    Task SubscribeJobEventsForAllJobSteps()
    {
        _subscriptionTimer = RegisterTimer(
            async (_) =>
            {
                foreach (var jobStep in _jobStepGrains.Values)
                {
                    await ThisJob.Subscribe(jobStep.Grain.AsReference<IJobObserver>());
                }
            },
            null!,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(10));

        return Task.CompletedTask;
    }

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
                await StatusChanged(JobStatus.CompletedWithFailures);
            }
            else
            {
                await StatusChanged(JobStatus.CompletedSuccessfully);
            }

            State.Remove = RemoveAfterCompleted;

            DeactivateOnIdle();
        }
    }

    static class JobStepInvoker
    {
        public static readonly MethodInfo StartMethod = typeof(JobStepInvoker).GetMethod(nameof(Start))!;
        public static readonly MethodInfo PrepareMethod = typeof(JobStepInvoker).GetMethod(nameof(Prepare))!;
        public static Task Start<TJobStepRequest>(IJobStep<TJobStepRequest> jobStep, GrainId jobId, TJobStepRequest request) => jobStep.Start(jobId, request);
        public static Task Prepare<TJobStepRequest>(IJobStep<TJobStepRequest> jobStep, TJobStepRequest request) => jobStep.Prepare(request);
    }
}
