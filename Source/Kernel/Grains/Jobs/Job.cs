// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Orleans.Observers;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Aksio.Reflection;
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
    where TRequest : class
    where TJobState : JobState
{
    IDictionary<JobStepId, JobStepGrainAndRequest> _jobStepGrains = new Dictionary<JobStepId, JobStepGrainAndRequest>();
    ObserverManager<IJobObserver>? _observers;
    bool _isRunning;
    IDisposable? _subscriptionTimer;
    JsonSerializerOptions? _jsonSerializerOptions;
    ILogger<IJob>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Job{TRequest, TJobState}"/> class.
    /// </summary>
    protected Job()
    {
        ThisJob = this;
    }

    /// <summary>
    /// Gets the <see cref="JobId"/> for this job.
    /// </summary>
    protected JobId JobId { get; private set; } = JobId.NotSet;

    /// <summary>
    /// Gets the <see cref="JobKey"/> for this job.
    /// </summary>
    protected JobKey JobKey { get; private set; } = JobKey.NotSet;

    /// <summary>
    /// Gets whether or not to clean up data after the job has completed.
    /// </summary>
    protected virtual bool RemoveAfterCompleted => false;

    /// <summary>
    /// Gets the job as a Grain reference.
    /// </summary>
    protected IJob<TRequest> ThisJob { get; private set; }

    /// <summary>
    /// Gets the request associated with the job.
    /// </summary>
    protected TRequest Request => (State.Request as TRequest)!;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger = ServiceProvider.GetService<ILogger<Job<TRequest, TJobState>>>() ?? new NullLogger<Job<TRequest, TJobState>>();

        _observers = new(
            TimeSpan.FromMinutes(1),
            ServiceProvider.GetService<ILogger<ObserverManager<IJobObserver>>>() ?? new NullLogger<ObserverManager<IJobObserver>>(),
            "JobObservers");

        _jsonSerializerOptions = ServiceProvider.GetService<JsonSerializerOptions>() ?? new JsonSerializerOptions();

        JobId = this.GetPrimaryKey(out var keyExtension);
        JobKey = (JobKey)keyExtension;

        ThisJob = GrainFactory.GetReference<IJob<TRequest>>(this);

        State.Name = GetType().Name;
        State.Type = this.GetGrainType();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Start(TRequest request)
    {
        using var scope = _logger?.BeginJobScope(JobId, JobKey);
        _logger?.Starting();

        await StatusChanged(JobStatus.Preparing);

        _isRunning = true;
        State.Request = request!;
        State.Details = GetJobDetails();
        await WriteStateAsync();

        var grainId = this.GetGrainId();
        var tcs = new TaskCompletionSource<IImmutableList<JobStepDetails>>();

        PrepareAllSteps(request, tcs);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        tcs.Task.ContinueWith(
            async (Task<IImmutableList<JobStepDetails>> jobStepsTask) =>
            {
                var jobSteps = await jobStepsTask;
                if (jobSteps.Count == 0)
                {
                    await OnCompleted();
                    await StatusChanged(JobStatus.CompletedSuccessfully);
                    await ClearStateAsync();
                    return;
                }
                _jobStepGrains = CreateGrainsFromJobSteps(jobSteps);
                await StatusChanged(JobStatus.PreparingSteps);
                await WriteStateAsync();

                await SubscribeJobEventsForAllJobSteps();
                PrepareAndStartAllJobSteps(grainId);
            },
            TaskScheduler.Current);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    /// <inheritdoc/>
    public async Task Resume()
    {
        if (_isRunning) return;

        if (!await CanResume())
        {
            await StatusChanged(JobStatus.Paused);
            await WriteStateAsync();
            return;
        }

        using var scope = _logger?.BeginJobScope(JobId, JobKey);
        _logger?.Resuming();

        var stepStorage = ServiceProvider.GetRequiredService<IJobStepStorage>();
        var steps = await stepStorage.GetForJob(JobId, JobStepStatus.Scheduled, JobStepStatus.Running, JobStepStatus.Paused);
        foreach (var step in steps)
        {
            var jobStep = (GrainFactory.GetGrain((Type)step.Type, step.Id.JobStepId, keyExtension: new JobStepKey(JobId, JobKey.MicroserviceId, JobKey.TenantId)) as IJobStep)!;
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

        using var scope = _logger?.BeginJobScope(JobId, JobKey);
        _logger?.Pausing();

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

        using var scope = _logger?.BeginJobScope(JobId, JobKey);
        _logger?.Stopping();

        _observers?.Notify(_ => _.OnJobStopped());

        var stepStorage = ServiceProvider.GetRequiredService<IJobStepStorage>();
        await stepStorage.RemoveAllForJob(JobId);

        if (State.Status > JobStatus.None && State.Status < JobStatus.CompletedSuccessfully)
        {
            await OnCompleted();
            await StatusChanged(JobStatus.Stopped);
            await WriteStateAsync();
        }

        _subscriptionTimer?.Dispose();
    }

    /// <inheritdoc/>
    public virtual Task OnCompleted() => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task OnStepSucceeded(JobStepId stepId, JobStepResult result)
    {
        State.Progress.SuccessfulSteps++;

        using var scope = _logger?.BeginJobScope(JobId, JobKey);
        _logger?.StepSuccessfullyCompleted(stepId);

        if (result.Result is JsonElement jsonResult)
        {
            result = result with { Result = jsonResult.Deserialize(_jobStepGrains[stepId].ResultType, _jsonSerializerOptions) };
        }

        await OnStepCompleted(stepId, result);
        if (!await HandleCompletion())
        {
            await WriteStateAsync();
        }

        await Unsubscribe(_jobStepGrains[stepId].Grain.AsReference<IJobObserver>());
        _jobStepGrains.Remove(stepId);
    }

    /// <inheritdoc/>
    public Task OnStepStopped(JobStepId stepId, JobStepResult result) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task OnStepFailed(JobStepId stepId, JobStepResult result)
    {
        State.Progress.FailedSteps++;

        using var scope = _logger?.BeginJobScope(JobId, JobKey);
        _logger?.StepFailed(stepId);

        await OnStepCompleted(stepId, result);
        if (!await HandleCompletion())
        {
            await WriteStateAsync();
        }

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
    public Task StatusChanged(JobStatus status, Exception? exception = null)
    {
        State.StatusChanges.Add(new JobStatusChanged
        {
            Status = status,
            Occurred = DateTimeOffset.UtcNow,
            ExceptionStackTrace = exception?.StackTrace ?? string.Empty,
            ExceptionMessages = exception?.GetAllMessages() ?? Enumerable.Empty<string>()
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
    /// Called when a step has completed.
    /// </summary>
    /// <param name="jobStepId"><see cref="JobStepId"/> for the completed step.</param>
    /// <param name="result"><see cref="JobStepResult"/> for the completed step.</param>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnStepCompleted(JobStepId jobStepId, JobStepResult result) => Task.CompletedTask;

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
        var jobStepType = typeof(TJobStep)
                            .AllBaseAndImplementingTypes()
                            .First(
                                _ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IJobStep<,>));
        var resultType = jobStepType.GetGenericArguments()[1];
        return new(
            typeof(TJobStep),
            jobStepId,
            new JobStepKey(jobId, jobKey.MicroserviceId, jobKey.TenantId),
            request,
            resultType);
    }

    /// <summary>
    /// Start the job.
    /// </summary>
    /// <param name="request">The request associated with the job.</param>
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
    /// <returns>The <see cref="JobDetails"/>.</returns>
    protected virtual JobDetails GetJobDetails() => JobDetails.NotSet;

    IDictionary<JobStepId, JobStepGrainAndRequest> CreateGrainsFromJobSteps(IImmutableList<JobStepDetails> jobSteps) =>
        jobSteps.ToDictionary(
            _ => _.Id,
            _ => new JobStepGrainAndRequest(
                (GrainFactory.GetGrain(_.Type, _.Id, keyExtension: _.Key) as IJobStep)!,
                _.Request,
                _.ResultType));
    void PrepareAllSteps(TRequest request, TaskCompletionSource<IImmutableList<JobStepDetails>> tcs) => _ = Task.Run(async () =>
    {
        var steps = await PrepareSteps(request);
        await ThisJob.SetTotalSteps(steps.Count);
        await ThisJob.WriteState();
        tcs.SetResult(steps);
    });

    void PrepareAndStartAllJobSteps(GrainId grainId) => _ = Task.Run(async () =>
    {
        using var scope = _logger?.BeginJobScope(JobId, JobKey);
        _logger?.PrepareJobStepsForRunning();

        await ThisJob.StatusChanged(JobStatus.PreparingStepsForRunning);

        try
        {
            await PrepareJobStepsForRunning();
        }
        catch (Exception ex)
        {
            _logger?.Failed(ex);

            await ThisJob.StatusChanged(JobStatus.Failed, ex);
            await ThisJob.WriteState();
            return;
        }

        await ThisJob.StatusChanged(JobStatus.StartingSteps);
        await ThisJob.WriteState();

        try
        {
            await StartAllJobSteps(grainId);

            await ThisJob.StatusChanged(JobStatus.Running);
            await ThisJob.WriteState();
        }
        catch (Exception ex)
        {
            _logger?.Failed(ex);

            await ThisJob.StatusChanged(JobStatus.Failed, ex);
            await ThisJob.WriteState();
        }
    });

    async Task StartAllJobSteps(GrainId grainId)
    {
        foreach (var jobStep in _jobStepGrains.Values)
        {
            await InvokeJobStepMethod(jobStep, JobStepInvoker.StartMethod, grainId, jobStep.Request);
        }
    }

    async Task PrepareJobStepsForRunning()
    {
        foreach (var jobStep in _jobStepGrains.Values)
        {
            await InvokeJobStepMethod(jobStep, JobStepInvoker.PrepareMethod, jobStep.Request);
        }
    }

    async Task InvokeJobStepMethod(JobStepGrainAndRequest jobStep, MethodInfo method, params object[] parameters)
    {
        var requestType = jobStep.Request.GetType();
        var jobStepType = typeof(IJobStep<,>).MakeGenericType(requestType, jobStep.ResultType);
        var jobStepReference = jobStep.Grain.AsReference(jobStepType);

        var task = method.GetGenericMethodDefinition()
            .MakeGenericMethod(requestType, jobStep.ResultType)
            .Invoke(null, new object[] { jobStepReference }.Concat(parameters).ToArray()) as Task;

        await task!;
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

    async Task<bool> HandleCompletion()
    {
        var cleared = false;

        if (State.Progress.IsCompleted)
        {
            var id = this.GetPrimaryKey(out var keyExtension);
            var key = (JobKey)keyExtension!;
            await OnCompleted();

            if (State.Progress.FailedSteps > 0)
            {
                await StatusChanged(JobStatus.CompletedWithFailures);
            }
            else
            {
                await StatusChanged(JobStatus.CompletedSuccessfully);
            }

            await GrainFactory
                    .GetGrain<IJobsManager>(0, new JobsManagerKey(key.MicroserviceId, key.TenantId))
                    .OnCompleted(id, State.Status);

            if (RemoveAfterCompleted)
            {
                await ClearStateAsync();
                cleared = true;
            }

            DeactivateOnIdle();
        }

        return cleared;
    }

    static class JobStepInvoker
    {
        public static readonly MethodInfo StartMethod = typeof(JobStepInvoker).GetMethod(nameof(Start))!;
        public static readonly MethodInfo PrepareMethod = typeof(JobStepInvoker).GetMethod(nameof(Prepare))!;
        public static Task Start<TJobStepRequest, TResult>(IJobStep<TJobStepRequest, TResult> jobStep, GrainId jobId, TJobStepRequest request) => jobStep.Start(jobId, request);
        public static Task Prepare<TJobStepRequest, TResult>(IJobStep<TJobStepRequest, TResult> jobStep, TJobStepRequest request) => jobStep.Prepare(request);
    }
}
