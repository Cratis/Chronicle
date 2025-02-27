// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
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
        DelayDeactivation(TimeSpan.MaxValue);

        _logger = ServiceProvider.GetService<ILogger<Job<TRequest, TJobState>>>() ?? new NullLogger<Job<TRequest, TJobState>>();
        _observers = new(
            TimeSpan.FromMinutes(1),
            ServiceProvider.GetService<ILogger<ObserverManager<IJobObserver>>>() ?? new NullLogger<ObserverManager<IJobObserver>>());

        JobId = this.GetPrimaryKey(out var keyExtension);
        JobKey = keyExtension;
        ThisJob = GrainFactory.GetReference<IJob<TRequest>>(this);
        State.Type = ServiceProvider.GetRequiredService<IJobTypes>().GetFor(this.GetGrainType()).Match(
            jobType => jobType,
            error => error switch
            {
                IJobTypes.GetForError.NoAssociatedJobType => throw new JobTypeNotAssociatedWithType(GetType()),
                _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
            });
        Storage = ServiceProvider.GetRequiredService<IStorage>()
            .GetEventStore(JobKey.EventStore)
            .GetNamespace(JobKey.Namespace);

        if (JobIsPrepared())
        {
            _jobStepGrains = await GetIdAndGrainReferenceToNonCompletedJobSteps();
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
        if (IsCompleted())
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
        return await PrepareAndStartRunningAllSteps(request);
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

            if (IsCompleted())
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

            _logger.Resuming();
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
                var failedStep = await HandleStartJobStepResult(id, result, grain);
                if (failedStep)
                {
                    failedSteps.Add(id);
                }
            }

            _ = await WriteStatusChanged(JobStatus.Running);
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
    public async Task<Result<PauseJobError>> Pause()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (State.Status is JobStatus.Failed)
            {
                return PauseJobError.Unknown;
            }
            if (IsCompleted() || State.Status is JobStatus.Stopped)
            {
                return PauseJobError.JobIsCompleted;
            }
            if (!JobIsRunning())
            {
                return PauseJobError.JobIsNotRunning;
            }

            _logger.Pausing();
            await _observers!.Notify(o => o.OnJobPaused());
            foreach (var (_, jobStepGrain) in _jobStepGrains)
            {
                await UnsubscribeJobStep(jobStepGrain.AsReference<IJobObserver>());
            }

            _ = await WriteStatusChanged(JobStatus.Paused);
            return Result<PauseJobError>.Success();
        }
        catch (Exception e)
        {
            _logger.Failed(e);
            return PauseJobError.Unknown;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<JobError>> Stop()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (State.Status is JobStatus.Failed)
            {
                return JobError.UnknownError;
            }
            if (IsCompleted() || State.Status is JobStatus.Stopped)
            {
                return JobError.JobIsStoppedOrCompleted;
            }
            if (!JobIsRunning() && State.Status != JobStatus.Paused)
            {
                return JobError.JobIsStoppedOrCompleted;
            }

            _logger.Stopping();

            // Set status Stopped so that it can be used in the OnCompleted if necessary
            _ = await WriteStatusChanged(JobStatus.Stopped);

            await _observers!.Notify(o => o.OnJobStopped());
            return Result<JobError>.Success();
        }
        catch (Exception e)
        {
            _logger.Failed(e);
            return JobError.UnknownError;
        }
    }

    /// <summary>
    /// What needs to be done by the Job implementation when all job steps are completed.
    /// </summary>
    /// <remarks>
    /// Job step being completed means either successfully completed, failed, partially failed or stopped/paused.
    /// </remarks>
    /// <returns>A task representing the operation.</returns>
    protected virtual Task OnCompleted() => Task.FromResult(Result.Success<JobError>());

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

    async Task<bool> HandleStartJobStepResult(JobStepId jobStepId, Result<StartJobStepError> result, IJobStep jobStepGrain)
    {
        try
        {
            if (result.TryGetError(out var error))
            {
                if (error is StartJobStepError.AlreadyRunning)
                {
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
            if (!State.Progress.IsCompleted)
            {
                return HandleCompletionSuccess.AllStepsNotCompletedYet;
            }

            var onCompletedResult = await Complete();
            if (onCompletedResult.TryGetError(out var onCompletedError))
            {
                // TODO: Should we just log and continue here?
                return onCompletedError;
            }
            if (!KeepAfterCompleted) // TODO: Should we always keep state if some or all steps, or job, failed?
            {
                await ClearStateAsync();
            }
            else if (State.Status is not JobStatus.Stopped)
            {
                StatusChanged(State.Progress.FailedSteps > 0 ? JobStatus.CompletedWithFailures : JobStatus.CompletedSuccessfully);
            }

            DeactivateOnIdle();
            return !KeepAfterCompleted ? HandleCompletionSuccess.ClearedState : HandleCompletionSuccess.NotClearedState;
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return JobError.UnknownError;
        }
    }

    async Task<Result<JobError>> Complete()
    {
        try
        {
            if (!AllStepsCompletedSuccessfully)
            {
                _logger.AllStepsNotCompletedSuccessfully();
            }
            await OnCompleted();
            return Result<JobError>.Success();
        }
        catch (Exception e)
        {
            _logger.FailedOnCompleted(e, State.Id, State.Type);
            return Result.Failed(JobError.UnknownError);
        }
    }
}
