// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Workers;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStep{TRequest, TResult}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request for the step.</typeparam>
/// <typeparam name="TResult">Type of result for the step.</typeparam>
/// <typeparam name="TState">Type of state for the step.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="JobStep{TRequest, TResult, TState}"/> class.
/// </remarks>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="logger">The logger.</param>
[KeepAlive]
public abstract class JobStep<TRequest, TResult, TState>(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<TState> state,
    ILogger<JobStep<TRequest, TResult, TState>> logger) : CpuBoundWorker<TRequest, JobStepResult>, IJobStep<TRequest, TResult>, IJobObserver, IDisposable
    where TState : JobStepState
{
    readonly CancellationTokenSource _cancellationTokenSource = new();
    bool _running;

    /// <summary>
    /// Gets the <see cref="JobStepId"/> for this job step.
    /// </summary>
    public JobStepId JobStepId => this.GetPrimaryKey();

    /// <summary>
    /// Gets the parent job.
    /// </summary>
    protected IJob Job { get; private set; } = new NullJob();

    /// <summary>
    /// Gets the job step as a Grain reference.
    /// </summary>
    protected IJobStep<TRequest, TResult> ThisJobStep { get; private set; } = null!;

    /// <summary>
    /// Gets the state for the job step.
    /// </summary>
    protected TState State => state.State;

    /// <summary>
    /// Gets the <see cref="JobStepIdentifier"/>.
    /// </summary>
    protected JobStepIdentifier Identifier { get; private set; } = null!;

    /// <summary>
    /// Gets the <see cref="JobStepName"/>.
    /// </summary>
    protected JobStepName Name { get; private set; } = null!;

    /// <inheritdoc/>
    public void Dispose() => _cancellationTokenSource.Dispose();

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);

        _ = this.GetPrimaryKey(out var key);
        var jobStepKey = (JobStepKey)key;
        var grainType = this.GetGrainType();
        Name = GetType().Name;
        Identifier = new(jobStepKey.JobId, JobStepId);

        state.State.Name = Name;
        state.State.Id = Identifier;
        state.State.Type = grainType;
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepPrepareStartError>> Start(GrainId jobId, object request)
    {
        using var scope = logger.BeginJobStepScope(State);
        var task = request switch
        {
            TRequest tRequest => Start(jobId, tRequest),
            _ => Task.FromResult<Result<JobStepPrepareStartError>>(JobStepPrepareStartError.WrongRequestType)
        };
        return await task;
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepPrepareStartError>> Prepare(object request)
    {
        using var scope = logger.BeginJobStepScope(State);
        var task = request switch
        {
            TRequest tRequest => Prepare(tRequest),
            _ => Task.FromResult<Result<JobStepPrepareStartError>>(JobStepPrepareStartError.WrongRequestType)
        };
        return await task;
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepPrepareStartError>> Start(GrainId jobId, TRequest request)
    {
        using var scope = logger.BeginJobStepScope(State);
        try
        {
            _running = true;
            Job = GrainFactory.GetGrain(jobId).AsReference<IJob>();
            ThisJobStep = GetReferenceToSelf<IJobStep<TRequest, TResult>>();

            await Start(request, _cancellationTokenSource.Token);
            var writeStateResult = await WriteStatusChange(JobStepStatus.Running);
            return writeStateResult.Match(
                _ => Result<JobStepPrepareStartError>.Success(),
                _ => JobStepPrepareStartError.FailedPersistingState);
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return JobStepPrepareStartError.Unknown;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepError>> Pause()
    {
        using var scope = logger.BeginJobStepScope(State);
        try
        {
            await _cancellationTokenSource.CancelAsync();
            await Job.Unsubscribe(this.AsReference<IJobObserver>());
            return await WriteStatusChange(JobStepStatus.Paused);
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return JobStepError.Unknown;
        }
    }

    /// <inheritdoc/>
    public Task<Result<JobStepResumeSuccess, JobStepError>> Resume(GrainId grainId)
    {
        using var scope = logger.BeginJobStepScope(State);
        try
        {
            var successType = _running
                ? JobStepResumeSuccess.AlreadyRunning
                : JobStepResumeSuccess.Success;
            return Task.FromResult(Result<JobStepResumeSuccess, JobStepError>.Success(successType));
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return Task.FromResult(Result<JobStepResumeSuccess, JobStepError>.Failed(JobStepError.Unknown));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepError>> Stop()
    {
        using var scope = logger.BeginJobStepScope(State);
        try
        {
            await _cancellationTokenSource.CancelAsync();
            await Job.Unsubscribe(this.AsReference<IJobObserver>());
            return await WriteStatusChange(JobStepStatus.Stopped);
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return JobStepError.Unknown;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepError>> ReportStatusChange(JobStepStatus status)
    {
        using var scope = logger.BeginJobStepScope(State);
        return await WriteStatusChange(status);
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepError>> ReportFailure(PerformJobStepError error)
    {
        using var scope = logger.BeginJobStepScope(State);
        try
        {
            var onStepFailedResult = await Job.OnStepFailed(JobStepId, JobStepResult.Failed(error));
            return await onStepFailedResult.Match(
                _ => WriteStatusChange(JobStepStatus.Failed, error.ErrorMessages, error.ExceptionStackTrace),
                onStepFailedError =>
                {
                    logger.FailedReportJobStepFailure(onStepFailedError);
                    return Task.FromResult(Result.Failed(JobStepError.FailedToReportToJob));
                });
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return JobStepError.Unknown;
        }
    }

    /// <summary>
    /// Prepare the step before it starts.
    /// </summary>
    /// <param name="request">The request object for the step.</param>
    /// <returns>Awaitable task.</returns>
    public virtual Task<Result<JobStepPrepareStartError>> Prepare(TRequest request) =>
        Task.FromResult(Result.Success<JobStepPrepareStartError>());

    /// <inheritdoc/>
    public async Task OnJobStopped()
    {
        await _cancellationTokenSource.CancelAsync();
    }

    /// <inheritdoc/>
    public async Task OnJobPaused()
    {
        await _cancellationTokenSource.CancelAsync();
    }

    /// <summary>
    /// The method that gets called when the step should do its work.
    /// </summary>
    /// <param name="request">The request object for the step.</param>
    /// <param name="cancellationToken">Cancellation token that can cancel the step work.</param>
    /// <returns>True if successful, false if not.</returns>
    protected abstract Task<Catch<JobStepResult>> PerformStep(TRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a grain reference to self.
    /// </summary>
    /// <typeparam name="TGrain">The type of the grain.</typeparam>
    /// <returns>The grain reference.</returns>
    protected TGrain GetReferenceToSelf<TGrain>()
        where TGrain : IJobStep<TRequest, TResult>
        => GrainFactory.GetGrain(GrainReference.GrainId).AsReference<TGrain>();

    /// <inheritdoc/>
    protected override async Task<PerformWorkResult<JobStepResult>> PerformWork(TRequest request)
    {
        // Important! State cannot be accessed or persisted directly, we need do that through the grain reference...
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            return new()
            {
                Error = PerformWorkError.Cancelled
            };
        }
        using var scope = logger.BeginJobStepScope(Identifier, Name, JobStepStatus.Running);

        // TODO: Should we do something here if it fails?
        _ = await ThisJobStep.ReportStatusChange(JobStepStatus.Running);
        var performStepResult = await PerformStep(request, _cancellationTokenSource.Token);
        var result = await performStepResult.Match(HandleJobStepResult, HandleException);
        DeactivateOnIdle();
        return result;

        async Task<PerformWorkResult<JobStepResult>> HandleJobStepResult(JobStepResult jobStepResult)
        {
            var performWorkResult = new PerformWorkResult<JobStepResult>
            {
                Result = jobStepResult
            };
            if (!jobStepResult.TryGetError(out var error))
            {
                if ((await Job.OnStepSucceeded(JobStepId, jobStepResult)).TryGetError(out var errorReportingSuccess))
                {
                    logger.FailedReportJobStepSuccess(errorReportingSuccess);
                    performWorkResult = performWorkResult with
                    {
                        Error = PerformWorkError.WorkerError
                    };
                }

                if ((await ThisJobStep.ReportStatusChange(JobStepStatus.Succeeded)).TryGetError(out var errorChangingStatus))
                {
                    logger.PerformingWorkFailedPersistState(errorChangingStatus);
                    performWorkResult = performWorkResult with
                    {
                        Error = PerformWorkError.WorkerError
                    };
                }
            }
            else
            {
                performWorkResult = performWorkResult with
                {
                    Error = error.Cancelled ? PerformWorkError.Cancelled : PerformWorkError.PerformingWorkError
                };

                if ((await ReportError(error)).TryGetError(out var errorReportingFailure))
                {
#pragma warning disable CA1848 // This will rarely happen.
                    logger.LogWarning("Failed to report that that the performing of job step was cancelled. Error: {ReportError}", errorReportingFailure);
#pragma warning restore CA1848
                    performWorkResult = performWorkResult with
                    {
                        Error = errorReportingFailure
                    };
                }
            }
            return performWorkResult;
        }

        async Task<PerformWorkResult<JobStepResult>> HandleException(Exception ex)
        {
            var performWorkResult = new PerformWorkResult<JobStepResult>
            {
                Exception = ex,
                Error = PerformWorkError.PerformingWorkError
            };
            logger.HandleUnexpectedPerformJobStepFailure(ex);

            // Deliberately don't handle error response from this call as it will be logged anyway.
            await ReportError(PerformJobStepError.Failed(ex));
            return performWorkResult;
        }

        async Task<Result<PerformWorkError>> ReportError(PerformJobStepError error)
        {
            logger.ReportFailurePerformingWork();
            var reportFailureResult = await ThisJobStep.ReportFailure(error);
            return reportFailureResult.Match(_ => Result<PerformWorkError>.Success(), _ => PerformWorkError.WorkerError);
        }
    }

    /// <summary>
    /// Saves the current grain state.
    /// </summary>
    /// <returns>A task representing the asynchronous action.</returns>
    protected async Task<Catch> WriteStateAsync()
    {
        try
        {
            await state.WriteStateAsync();
            return Catch.Success();
        }
        catch (Exception ex)
        {
            logger.FailedToWriteState(ex);
            return ex;
        }
    }

    JobStepError HandleFailedToWriteStatusChange(Exception ex, JobStepStatus status)
    {
        logger.FailedToWriteState(ex, status);
        return JobStepError.FailedToPersistState;
    }

    async Task<Result<JobStepError>> WriteStatusChange(JobStepStatus status, IEnumerable<string>? exceptionMessages = null!, string? exceptionStackTrace = null!)
    {
        try
        {
            logger.ChangingStatus(status);
            StatusChanged(status, exceptionMessages, exceptionStackTrace);
            var writeResult = await WriteStateAsync();
            return writeResult.Match(
                _ => Result<JobStepError>.Success(),
                ex => HandleFailedToWriteStatusChange(ex, status));
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return JobStepError.Unknown;
        }
    }

    void StatusChanged(JobStepStatus status, IEnumerable<string>? exceptionMessages = null!, string? exceptionStackTrace = null!)
    {
        state.State.StatusChanges.Add(new()
        {
            Status = status,
            Occurred = DateTimeOffset.UtcNow,
            ExceptionMessages = exceptionMessages ?? [],
            ExceptionStackTrace = exceptionStackTrace ?? string.Empty
        });
        state.State.Status = status;
    }
}
