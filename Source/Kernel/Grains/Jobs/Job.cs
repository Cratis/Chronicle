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
    Dictionary<JobStepId, JobStepGrainAndRequest> _jobStepGrains = [];
    ObserverManager<IJobObserver>? _observers;
    bool _isRunning;
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
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger = ServiceProvider.GetService<ILogger<Job<TRequest, TJobState>>>() ?? new NullLogger<Job<TRequest, TJobState>>();
        _observers = new(
            TimeSpan.FromMinutes(1),
            ServiceProvider.GetService<ILogger<ObserverManager<IJobObserver>>>() ?? new NullLogger<ObserverManager<IJobObserver>>());

        JobId = this.GetPrimaryKey(out var keyExtension);
        JobKey = keyExtension;
        ThisJob = GrainFactory.GetReference<IJob<TRequest>>(this);
        State.Type = ServiceProvider.GetRequiredService<IJobTypes>().GetFor(GetType()).Match(
            jobType => jobType,
            error => error switch
            {
                IJobTypes.GetForError.NoAssociatedJobType => throw new JobTypeNotAssociatedWithType(GetType()),
                _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
            });
        Storage = ServiceProvider.GetRequiredService<IStorage>()
            .GetEventStore(JobKey.EventStore)
            .GetNamespace(JobKey.Namespace);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<Result<StartJobError>> Start(TRequest request)
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        _logger.Starting();
        _isRunning = true;

        State.Created = DateTimeOffset.UtcNow;
        State.Request = request!;
        State.Details = GetJobDetails();

        _ = await WriteStatusChanged(JobStatus.PreparingSteps);
        return await PrepareAndStartRunningAllSteps(request);
    }

    /// <inheritdoc/>
    public async Task<Result<ResumeJobSuccess, ResumeJobError>> Resume()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (_isRunning)
            {
                return ResumeJobSuccess.JobAlreadyRunning;
            }

            if (!await CanResume())
            {
                return ResumeJobSuccess.JobCannotBeResumed;
            }

            _logger.Resuming();
            _isRunning = true;

            var getStepsResult = await Storage.JobSteps.GetForJob(JobId, JobStepStatus.Scheduled, JobStepStatus.Running, JobStepStatus.Paused);
            return await getStepsResult.Match(
                HandleResumeSteps,
                ex =>
                {
                    _logger.FailedToGetJobSteps(ex);
                    return Task.FromResult(Result.Failed<ResumeJobSuccess, ResumeJobError>(JobError.StorageError));
                });
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(JobError.UnknownError);
        }

        async Task<Result<ResumeJobSuccess, ResumeJobError>> HandleResumeSteps(IEnumerable<JobStepState> steps)
        {
            var tasks = steps
                .Select(step =>
                {
                    var grain = (GrainFactory.GetGrain(
                        (Type)step.Type,
                        step.Id.JobStepId,
                        keyExtension: new JobStepKey(JobId, JobKey.EventStore, JobKey.Namespace)) as IJobStep)!;
                    return new
                    {
                        Id = step.Id.JobStepId,
                        Grain = grain
                    };
                })
                .Select(async jobStep =>
                {
                    var result = await jobStep.Grain.Start(this.GetGrainId());
                    return new
                    {
                        jobStep.Id,
                        Result = result
                    };
                });
            var results = await Task.WhenAll(tasks);
            if (results.Any(resumeJob => !resumeJob.Result.IsSuccess))
            {
                return Result.Failed<ResumeJobSuccess, ResumeJobError>(
                    new FailedResumingJobSteps(results.Where(resumeJob => !resumeJob.Result.IsSuccess).Select(resumeJob => resumeJob.Id)));
            }
            return ResumeJobSuccess.Success;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<JobError>> Pause()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (IsInStoppedOrCompletedState())
            {
                return JobError.JobIsStoppedOrCompleted;
            }
            if (!JobIsRunning())
            {
                return JobError.JobIsStoppedOrCompleted;
            }

            _logger.Pausing();

            await _observers!.Notify(o => o.OnJobPaused());
            return await WriteStatusChanged(JobStatus.Paused);
        }
        catch (Exception e)
        {
            _logger.Failed(e);
            return JobError.UnknownError;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<JobError>> Stop()
    {
        using var scope = _logger.BeginJobScope(JobId, JobKey);
        try
        {
            if (IsInStoppedOrCompletedState())
            {
                return JobError.JobIsStoppedOrCompleted;
            }
            if (!JobIsRunning() && State.Status != JobStatus.Paused)
            {
                return JobError.JobIsStoppedOrCompleted;
            }

            _logger.Stopping();

            // Set status Stopped so that it can be used in the OnCompleted if necessary
            StatusChanged(JobStatus.Stopped);

            await _observers!.Notify(o => o.OnJobStopped());
            var stepStorage = ServiceProvider.GetRequiredService<IJobStepStorage>();
            // TODO: We probably don't want to do this. And we need to nail down the semantics of Stop vs Pause and how it works with Resume
            var removeAllStepsResult = await stepStorage.RemoveAllForJob(JobId);
            return await removeAllStepsResult.Match(
                async _ =>
                {
                    var onCompletedResult = await Complete();
                    return await onCompletedResult.Match(
                        _ => WriteStatusChanged(JobStatus.Stopped),
                        error => Task.FromResult(Result.Failed(error)));
                },
                ex =>
                {
                    _logger.FailedToRemoveForJob(ex);
                    return Task.FromResult(Result.Failed(JobError.StorageError));
                });
        }
        catch (Exception e)
        {
            _logger.Failed(e);
            return JobError.UnknownError;
        }
    }

    /// <inheritdoc/>
    public virtual Task OnCompleted() => Task.FromResult(Result.Success<JobError>());

    /// <inheritdoc/>
    public async Task<Result<JobError>> SetTotalSteps(int totalSteps)
    {
        _logger.BeginJobScope(JobId, JobKey);
        try
        {
            State.Progress.TotalSteps = totalSteps;
            var writeResult = await WriteState();
            return writeResult.Match(_ => Result.Success<JobError>(), ex =>
            {
                _logger.FailedToSetTotalSteps(ex);
                return JobError.PersistStateError;
            });
        }
        catch (Exception ex)
        {
            _logger.Failed(ex);
            return JobError.UnknownError;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<JobError>> WriteStatusChanged(JobStatus status, Exception? exception = null)
    {
        _logger.BeginJobScope(JobId, JobKey);
        try
        {
            _logger.ChangingStatus(status);
            StatusChanged(status, exception);
            var writeStateResult = await WriteState();
            return writeStateResult.Match(_ => Result.Success<JobError>(), _ =>
            {
                _logger.FailedWritingStatusChange(status);
                return JobError.PersistStateError;
            });
        }
        catch (Exception e)
        {
            _logger.Failed(e);
            return JobError.UnknownError;
        }
    }

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
    /// Writes persisted state.
    /// </summary>
    /// <returns>A task returning <see cref="Catch"/>.</returns>
    protected async Task<Catch> WriteState()
    {
        _logger.BeginJobScope(JobId, JobKey);
        try
        {
            await WriteStateAsync();
            return Catch.Success();
        }
        catch (Exception e)
        {
            _logger.FailedWritingState(e);
            return e;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the job is in a stopped or completed state.
    /// </summary>
    /// <returns>True if it is stopped or completed, false if not.</returns>
    protected bool IsInStoppedOrCompletedState() =>
        State.Status is JobStatus.Stopped or JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures;

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

    bool JobIsRunning() => State.Status is JobStatus.Running or JobStatus.PreparingSteps or JobStatus.PreparingStepsForRunning;

    void StatusChanged(JobStatus status, Exception? exception = null)
    {
        State.StatusChanges.Add(new()
        {
            Status = status,
            Occurred = DateTimeOffset.UtcNow,
            ExceptionStackTrace = exception?.StackTrace ?? string.Empty,
            ExceptionMessages = exception?.GetAllMessages() ?? []
        });
        State.Status = status;
    }

    async Task<Result<HandleCompletionSuccess, JobError>> HandleCompletion()
    {
        try
        {
            if (!State.Progress.IsCompleted)
            {
                return HandleCompletionSuccess.AllStepsNotCompletedYet;
            }

            var cleared = false;

            var onCompletedResult = await Complete();
            if (onCompletedResult.TryGetError(out var onCompletedError))
            {
                return onCompletedError;
            }
            if (!KeepAfterCompleted) // TODO: Should we always keep state if some or all steps, or job, failed?
            {
                await ClearStateAsync();
                cleared = true;
            }
            else
            {
                StatusChanged(State.Progress.FailedSteps > 0 ? JobStatus.CompletedWithFailures : JobStatus.CompletedSuccessfully);
            }

            DeactivateOnIdle();
            return cleared ? HandleCompletionSuccess.ClearedState : HandleCompletionSuccess.NotClearedState;
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
