// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Providers;
using Orleans.Utilities;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJob{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request object that gets passed to job.</typeparam>
/// <typeparam name="TJobState">Type of state for the job.</typeparam>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Jobs)]
public abstract partial class Job<TRequest, TJobState> : Grain<TJobState>, IJob<TRequest>
    where TRequest : class, IJobRequest
    where TJobState : JobState
{
    Dictionary<JobStepId, IJobStep>? _jobStepGrains;
    ObserverManager<IJobObserver>? _observers;
    ILogger<IJob> _logger = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="Job{TRequest, TJobState}"/> class.
    /// </summary>
    protected Job()
    {
        ThisJob = this;
        Storage = null!;
    }

    enum HandleCompletionSuccess
    {
        AllStepsNotCompletedYet = 0,
        ClearedState = 1,
        NotClearedState = 2
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
    /// Gets a value indicating whether to keep the persisted data after the job has completed.
    /// </summary>
    protected virtual bool KeepAfterCompleted => false;

    /// <summary>
    /// Gets the job as a Grain reference.
    /// </summary>
    protected IJob<TRequest> ThisJob { get; private set; }

    /// <summary>
    /// Gets the request associated with the job.
    /// </summary>
    protected TRequest Request => (State.Request as TRequest)!;

    /// <summary>
    /// Gets the underlying <see cref="IStorage"/>.
    /// </summary>
    protected IEventStoreNamespaceStorage Storage { get; private set; }

    /// <summary>
    /// Gets a value indicating whether all steps have been completed successfully.
    /// </summary>
    protected bool AllStepsCompletedSuccessfully => State.Progress.SuccessfulSteps == State.Progress.TotalSteps;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Keep the Grain alive forever: Confirmed here: https://github.com/dotnet/orleans/issues/1721#issuecomment-216566448
        DelayDeactivation(TimeSpan.FromDays(365 * 5));

        _logger = ServiceProvider.GetService<ILogger<Job<TRequest, TJobState>>>() ?? new NullLogger<Job<TRequest, TJobState>>();
        _observers = new(
            TimeSpan.FromDays(365 * 3),
            ServiceProvider.GetService<ILogger<ObserverManager<IJobObserver>>>() ?? new NullLogger<ObserverManager<IJobObserver>>());

        JobId = this.GetPrimaryKey(out var keyExtension);
        JobKey = keyExtension!;
        ThisJob = GrainFactory.GetReference<IJob<TRequest>>(this);
        State.Type = ServiceProvider.GetRequiredService<IJobTypes>().GetForOrThrow(this.GetGrainType());
        Storage = ServiceProvider.GetRequiredService<IStorage>()
            .GetEventStore(JobKey.EventStore)
            .GetNamespace(JobKey.Namespace);

        if (JobIsPrepared())
        {
            _jobStepGrains = await GetIdAndGrainReferenceForNonCompletedJobSteps();
        }
    }

    /// <inheritdoc/>
    public async Task<Result<StartJobError>> Start(TRequest request)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        _logger.Starting();
        if (State.Status is JobStatus.Failed)
        {
            return StartJobError.Unknown;
        }
        if (State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures)
        {
            return StartJobError.AlreadyCompleted;
        }
        if (JobIsPrepared())
        {
            return StartJobError.AlreadyBeenPrepared;
        }

        State.Created = DateTimeOffset.UtcNow;
        State.Request = request!;
        State.Details = GetJobDetails();

        _ = await WriteStatusChanged(JobStatus.PreparingJob);
        var prepareAndStartRunningAllStepsResult = await PrepareAndStartRunningAllSteps(request);
        if (prepareAndStartRunningAllStepsResult.TryGetError(out var error) && error is StartJobError.CouldNotPrepareJobSteps)
        {
            await OnFailedToPrepare();
        }
        return prepareAndStartRunningAllStepsResult;
    }

    /// <inheritdoc/>
    public async Task<Result<ResumeJobSuccess, ResumeJobError>> Resume()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (State.Status is JobStatus.Failed)
            {
                return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
            }
            if (JobIsRunning())
            {
                return ResumeJobSuccess.JobAlreadyRunning;
            }

            if (State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures)
            {
                return ResumeJobSuccess.JobIsCompleted;
            }

            if (!await CanResume())
            {
                return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobCannotBeResumed);
            }

            if (!JobIsPrepared())
            {
                return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobIsNotPrepared);
            }

            if (_jobStepGrains?.Count == 0)
            {
                _jobStepGrains = await GetIdAndGrainReferenceForNonCompletedJobSteps();
            }

            _logger.Resuming();
            State.Progress.StoppedSteps = 0;
            _ = await WriteStatusChanged(JobStatus.Running);
            await OnBeforeResumingJobSteps();
            var grainId = this.GetGrainId();

            var tasks = _jobStepGrains!.Select(async jobStepIdAndGrain =>
            {
                var result = await jobStepIdAndGrain.Value.Start(grainId);
                return (jobStepIdAndGrain.Key, result, jobStepIdAndGrain.Value);
            });
            var startJobStepResults = await Task.WhenAll(tasks);
            var failedSteps = new List<JobStepId>();

            foreach (var (id, result, grain) in startJobStepResults)
            {
                var failedStep = await HandleResumeJobStepResult(id, result, grain);
                if (failedStep)
                {
                    failedSteps.Add(id);
                }
            }

            return failedSteps.Count == 0
                ? ResumeJobSuccess.Success
                : Result<ResumeJobSuccess, ResumeJobError>.Failed(new FailedResumingJobSteps(failedSteps));
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<StopJobError>> Stop()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (State.Status is JobStatus.Failed)
            {
                return StopJobError.Unknown;
            }
            if (State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures)
            {
                return StopJobError.JobIsCompleted;
            }
            if (!JobIsRunning())
            {
                return StopJobError.JobIsNotRunning;
            }

            _logger.Stopping();
            _ = await WriteStatusChanged(JobStatus.Stopped);

            // Do not call OnStopped here because it should be called after all steps have been stopped
            await StopNonCompletedStepsAndEnsureCompletion(false);

            return Result<StopJobError>.Success();
        }
        catch (Exception e)
        {
            _logger.Failed(e);
            return StopJobError.Unknown;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<RemoveJobError>> Remove()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (State.Status is JobStatus.Failed)
            {
                return RemoveJobError.Unknown;
            }
            if (State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures)
            {
                return RemoveJobError.JobIsCompleted;
            }
            if (State.Status is JobStatus.Removing)
            {
                return RemoveJobError.JobIsAlreadyBeingRemoved;
            }

            _logger.Removing();
            _ = await WriteStatusChanged(JobStatus.Removing);

            // Do not call OnStopped here because it should be called after all steps have been stopped
            await StopNonCompletedStepsAndEnsureCompletion(true);
            return Result<RemoveJobError>.Success();
        }
        catch (Exception e)
        {
            _logger.Failed(e);
            return RemoveJobError.Unknown;
        }
    }

    /// <summary>
    /// What needs to be done by the Job implementation before starting all prepared job steps.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    protected virtual Task OnBeforeStartingJobSteps() => Task.CompletedTask;

    /// <summary>
    /// What needs to be done by the Job implementation before resuming all prepared and non-completed job steps.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    protected virtual Task OnBeforeResumingJobSteps() => Task.CompletedTask;

    /// <summary>
    /// What needs to be done by the Job implementation when all job steps are completed or job is being removed.
    /// </summary>
    /// <remarks>
    /// Job step being completed means either successfully completed, failed or partially failed.
    /// </remarks>
    /// <returns>A task representing the operation.</returns>
    protected virtual Task OnCompleted() => Task.CompletedTask;

    /// <summary>
    /// What needs to be done by the Job implementation when one or more job steps failed to prepare.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    protected virtual Task OnFailedToPrepare() => Task.CompletedTask;

    /// <summary>
    /// What needs to be done by the Job implementation when running job steps are stopped or removed manually.
    /// </summary>
    /// <remarks>
    /// This is also called when a Job is being removed.
    /// OnStopped can be called on a Job two or more times. For instance if a Job is first stopped then removed later.
    /// It will first call OnStopped when the non-completed steps are stopped and then call it again when Remove is called.
    /// </remarks>
    /// <returns>A task representing the operation.</returns>
    protected virtual Task OnStopped() => Task.FromResult(Result.Success<JobError>());

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

    async Task StopNonCompletedStepsAndEnsureCompletion(bool removing)
    {
        if (_observers?.Count > 0)
        {
            // The job steps will eventually handle completion
            await _observers!.Notify(o => removing ? o.OnJobRemoved() : o.OnJobStopped());
            return;
        }

        // This state can occur when we are trying to stop or remove a Job that is persisted over a system shutdown.
        // Then we want to resubscribe all non-completed job steps
        if (State.Progress is { IsCompleted: false, IsStopped: false })
        {
            _jobStepGrains ??= await GetIdAndGrainReferenceForNonCompletedJobSteps();
            foreach (var (jobStepId, _) in _jobStepGrains)
            {
                await SubscribeJobStep(_jobStepGrains![jobStepId].AsReference<IJobObserver>());
            }
        }

        if (_observers is null || _observers.Count == 0)
        {
            // All job steps should be in stopped state. Just HandleCompletion manually
            _ = await HandleCompletionResult(await HandleCompletion());
        }
        else
        {
            // The job steps will eventually handle completion
            await _observers!.Notify(o => removing ? o.OnJobRemoved() : o.OnJobStopped());
        }
    }

    async Task<bool> HandleResumeJobStepResult(JobStepId jobStepId, Result<StartJobStepError> result, IJobStep jobStepGrain)
    {
        try
        {
            if (result.TryGetError(out var error))
            {
                if (error is StartJobStepError.AlreadyStarted)
                {
                    await SubscribeJobStep(jobStepGrain.AsReference<IJobObserver>());
                    return true;
                }

                _logger.FailedStartingJobStep(jobStepId, error);
                return false;
            }
            await SubscribeJobStep(jobStepGrain.AsReference<IJobObserver>());
            return false;
        }
        catch (Exception ex)
        {
            _logger.FailedStartingJobStep(ex, jobStepId);
            return false;
        }
    }

    async Task<Result<HandleCompletionSuccess, JobError>> HandleCompletion()
    {
        try
        {
            if (State.Progress is { IsCompleted: false, IsStopped: false })
            {
                return HandleCompletionSuccess.AllStepsNotCompletedYet;
            }

            // When all steps have been stopped or completed
            if (State.Status is JobStatus.Stopped or JobStatus.Removing)
            {
                await OnStopped();
            }

            // Don't go to Complete if the job is Stopped, because it can be resumed or removed in the future.
            if (State.Status is JobStatus.Stopped)
            {
                return HandleCompletionSuccess.AllStepsNotCompletedYet;
            }

            var onCompletedResult = await Complete();
            if (onCompletedResult.TryGetException(out var onCompletedError))
            {
                StatusChanged(JobStatus.Failed, onCompletedError);
            }
            else if (State.Status is not JobStatus.Removing)
            {
                StatusChanged(State.Progress.FailedSteps > 0 ? JobStatus.CompletedWithFailures : JobStatus.CompletedSuccessfully);
            }
            var shouldClearState = State.Status is not JobStatus.Failed and not JobStatus.CompletedWithFailures &&
                                    (State.Status is JobStatus.Removing || !KeepAfterCompleted);
            if (shouldClearState)
            {
                await ClearStateAsync();
            }
            DeactivateOnIdle();
            return shouldClearState ? HandleCompletionSuccess.ClearedState : HandleCompletionSuccess.NotClearedState;
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return JobError.UnknownError;
        }
    }

    async Task<Catch> Complete()
    {
        try
        {
            if (!AllStepsCompletedSuccessfully)
            {
                _logger.AllStepsNotCompletedSuccessfully();
            }
            await OnCompleted();
            return Catch.Success();
        }
        catch (Exception ex)
        {
            _logger.FailedOnCompleted(ex, State.Id, State.Type);
            return ex;
        }
    }
}
