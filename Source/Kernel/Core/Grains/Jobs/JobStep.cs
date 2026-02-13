// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Grains.Workers;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStep{TRequest, TResult, TState}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request for the step.</typeparam>
/// <typeparam name="TResult">Type of result for the step.</typeparam>
/// <typeparam name="TState">Type of state for the step.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="JobStep{TRequest, TResult, TState}"/> class.
/// </remarks>
/// <param name="state"><see cref="IPersistentState{TState}"/> for managing state of the job step.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> for limiting parallel execution.</param>
/// <param name="logger">The logger.</param>
[KeepAlive]
public abstract class JobStep<TRequest, TResult, TState>(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<TState> state,
    IJobStepThrottle throttle,
    ILogger<JobStep<TRequest, TResult, TState>> logger) : GrainWithBackgroundTask<TRequest, JobStepResult>, IJobStep<TRequest, TResult, TState>, IJobObserver, IDisposable
    where TState : JobStepState
{
    IJobStep<TRequest, TResult, TState> _thisJobStep = null!;
    CancellationTokenSource? _cancellationTokenSource = new();
    IJob _job = new NullJob();
    bool _currentlyRunning;

    /// <summary>
    /// Gets the <see cref="JobStepId"/> for this job step.
    /// </summary>
    protected JobStepId JobStepId => this.GetPrimaryKey();

    /// <summary>
    /// Gets the parent job id.
    /// </summary>
    protected JobId JobId { get; private set; } = null!;

    /// <summary>
    /// Gets the state for the job step.
    /// </summary>
#pragma warning disable CA1721
    protected TState State => state.State;
#pragma warning restore CA1721

    /// <summary>
    /// Gets the <see cref="JobStepIdentifier"/>.
    /// </summary>
    protected JobStepIdentifier Identifier { get; private set; } = null!;

    /// <summary>
    /// Gets the <see cref="JobStepName"/>.
    /// </summary>
    protected JobStepName Name { get; private set; } = null!;

    /// <inheritdoc/>
    public void Dispose()
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);

        _ = this.GetPrimaryKey(out var key);
        var jobStepKey = (JobStepKey)key!;
        var grainType = this.GetGrainType();
        Name = GetType().Name;
        JobId = jobStepKey.JobId;
        Identifier = new(jobStepKey.JobId, JobStepId);

        state.State.Name = Name;
        state.State.Id = Identifier;
        state.State.Type = grainType;
        _cancellationTokenSource = new();
    }

    /// <inheritdoc/>
    public async Task<Result<StartJobStepError>> Start(GrainId jobGrainId)
    {
        using var scope = logger.BeginJobStepScope(State);
        try
        {
            if (!State.IsPrepared)
            {
                return StartJobStepError.NotPrepared;
            }

            switch (State.Status)
            {
                case JobStepStatus.Scheduled or JobStepStatus.Running:
                    return StartJobStepError.AlreadyStarted;
                case JobStepStatus.CompletedSuccessfully or JobStepStatus.CompletedWithFailure:
                    return StartJobStepError.Completed;
                case JobStepStatus.Failed:
                    return StartJobStepError.UnrecoverableFailedState;
            }
            _thisJobStep = this.AsReference<IJobStep<TRequest, TResult, TState>>();
            _job = GrainFactory.GetGrain<IJob>(jobGrainId);
            var scheduledWork = await Start(_cancellationTokenSource!.Token);
            if (scheduledWork)
            {
                _currentlyRunning = true;
                _ = await WriteStatusChange(JobStepStatus.Scheduled);
            }
            return scheduledWork ? Result.Success<StartJobStepError>() : StartJobStepError.AlreadyStarted;
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return StartJobStepError.Unknown;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<PrepareJobStepError>> Prepare(object request)
    {
        using var scope = logger.BeginJobStepScope(State);
        var prepareTask = request switch
        {
            TRequest tRequest => Prepare(tRequest),
            _ => Task.FromResult<Result<PrepareJobStepError>>(PrepareJobStepError.WrongRequestType)
        };
        var prepareResult = await prepareTask;
        if (!prepareResult.IsSuccess)
        {
            return prepareResult;
        }

        State.IsPrepared = true;
        _ = await WriteStateAsync();
        return prepareResult;
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepError>> Stop(bool removing)
    {
        using var scope = logger.BeginJobStepScope(State);
        try
        {
            if (!removing && State.Status is JobStepStatus.Stopped)
            {
                logger.AlreadyStopped();
                DeactivateOnIdle();
                return Result.Success<JobStepError>();
            }

            logger.Stopping();
            var cancelTokenTask = _cancellationTokenSource?.CancelAsync() ?? Task.CompletedTask;
            await cancelTokenTask;
            _cancellationTokenSource = null;
            if (_currentlyRunning)
            {
                return Result.Success<JobStepError>();
            }

            if (!State.IsPrepared)
            {
                return Result.Success<JobStepError>();
            }

            // Logic here is that if we stop a job step that is not currently running and maybe not even prepared we still want to go through the same job step completion process
            // Therefore we want to try to create a job step result from whatever the current state is and use that in the completion process.
            var result = await CreateCancelledResultFromCurrentState(State);
            await ReportFailure(result is not null
                ? PerformJobStepError.CancelledWithPartialResult(result)
                : PerformJobStepError.CancelledWithNoResult());
            return Result.Success<JobStepError>();
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            _ = await WriteStatusChange(JobStepStatus.Failed, ex.GetAllMessages(), ex.StackTrace);
            return JobStepError.Unknown;
        }
        finally
        {
            if (removing)
            {
                var wasStopped = State.Status == JobStepStatus.Stopped;
                _ = await WriteStatusChange(JobStepStatus.Removing);
                if (wasStopped)
                {
                    DeactivateOnIdle();
                }
            }
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
            var result = error.Cancelled
                ? await _job.OnStepStopped(JobStepId, JobStepResult.Failed(error))
                : await _job.OnStepFailed(JobStepId, JobStepResult.Failed(error));
            return await result.Match(
                _ => WriteStatusChange(error.Cancelled ? JobStepStatus.Stopped : JobStepStatus.CompletedWithFailure, error.ErrorMessages, error.ExceptionStackTrace),
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
    public async Task<Result<PrepareJobStepError>> Prepare(TRequest request)
    {
        try
        {
            return await (await PrepareStep(request)).Match(
                async none =>
                {
                    await InitializeState(request);
                    return Result<PrepareJobStepError>.Success();
                },
                async error =>
                {
                    _ = await WriteStatusChange(JobStepStatus.Failed, [error.ToString()]);
                    return Result.Failed(error);
                });
        }
        catch (Exception ex)
        {
            logger.FailedPreparing(ex, Name);
            _ = await WriteStatusChange(JobStepStatus.Failed, ex.GetAllMessages(), ex.StackTrace);
            return PrepareJobStepError.Unknown;
        }
    }

    /// <inheritdoc/>
    public Task<TState> GetState() => Task.FromResult(State);

    /// <inheritdoc/>
    public Task OnJobStopped() => Stop(false);

    /// <inheritdoc/>
    public Task OnJobRemoved() => Stop(true);

    /// <summary>
    /// Prepare the step before it starts.
    /// </summary>
    /// <param name="request">The request object for the step.</param>
    /// <returns>Awaitable task.</returns>
    protected abstract Task<Result<PrepareJobStepError>> PrepareStep(TRequest request);

    /// <summary>
    /// The method that gets called when the step should do its work.
    /// </summary>
    /// <remarks>When this is executed it is not within the Activation Context. If context needs to be accessed then it needs to be referenced indirectly through a reference to itself.</remarks>
    /// <param name="currentState">The current state of the job step.</param>
    /// <param name="cancellationToken">Cancellation token that can cancel the step work.</param>
    /// <returns>True if successful, false if not.</returns>
    protected abstract Task<Catch<JobStepResult>> PerformStep(TState currentState, CancellationToken cancellationToken);

    /// <summary>
    /// Initialize the state from the initial request arguments.
    /// </summary>
    /// <remarks>This method is invoked after <see cref="PrepareStep"/> successfully completed.</remarks>
    /// <param name="request">The <typeparamref name="TRequest"/> initial request arguments.</param>
    /// <returns>The <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    protected abstract ValueTask InitializeState(TRequest request);

    /// <inheritdoc/>
    protected override async Task<PerformWorkResult<JobStepResult>> PerformWork()
    {
        // Important! State cannot be accessed or persisted directly, we need do that through the grain reference...
        if (_cancellationTokenSource?.IsCancellationRequested ?? true)
        {
            return new()
            {
                Error = PerformWorkError.Cancelled
            };
        }

        // Acquire throttle slot before performing work
        try
        {
            await throttle.AcquireAsync(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            return new()
            {
                Error = PerformWorkError.Cancelled
            };
        }

        try
        {
            using var scope = logger.BeginJobStepScope(Identifier, Name, JobStepStatus.Running);

            // TODO: Should we do something here if it fails?
            _ = await _thisJobStep.ReportStatusChange(JobStepStatus.Running);
            var currentState = await _thisJobStep.GetState();
            var performStepResult = await PerformStep(currentState, _cancellationTokenSource.Token);
            return await performStepResult.Match(HandleJobStepResult, HandleException);
        }
        finally
        {
            // Always release throttle slot after work completes (success or failure)
            throttle.Release();
        }

        async Task<PerformWorkResult<JobStepResult>> HandleJobStepResult(JobStepResult jobStepResult)
        {
            var performWorkResult = new PerformWorkResult<JobStepResult>
            {
                Result = jobStepResult
            };
            try
            {
                if (!jobStepResult.TryGetError(out var error))
                {
                    if ((await _thisJobStep.ReportStatusChange(JobStepStatus.CompletedSuccessfully)).TryGetError(out var errorChangingStatus))
                    {
                        logger.PerformingWorkFailedPersistState(errorChangingStatus);
                        performWorkResult = performWorkResult with
                        {
                            Error = PerformWorkError.WorkerError
                        };
                        _ = await WriteStatusChange(JobStepStatus.Failed, ["Error reporting job step succeeded"]);
                    }

                    if ((await _job.OnStepSucceeded(JobStepId, jobStepResult)).TryGetError(out var errorReportingSuccess))
                    {
                        logger.FailedReportJobStepSuccess(errorReportingSuccess);
                        performWorkResult = performWorkResult with
                        {
                            Error = PerformWorkError.WorkerError
                        };
                        _ = await WriteStatusChange(JobStepStatus.Failed, ["Error reporting job step succeeded"]);
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
                        logger.FailedReportingJobStepCancelled(errorReportingFailure);
                        performWorkResult = performWorkResult with
                        {
                            Error = errorReportingFailure
                        };
                    }
                }
                return performWorkResult;
            }
            catch (Exception ex)
            {
                logger.FailedUnexpectedly(ex);
                return performWorkResult with
                {
                    Error = PerformWorkError.WorkerError
                };
            }
            finally
            {
                DeactivateOnIdle();
            }
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
            try
            {
                logger.ReportFailurePerformingWork();
                var reportFailureResult = await _thisJobStep.ReportFailure(error);
                return reportFailureResult.Match(_ => Result<PerformWorkError>.Success(), _ => PerformWorkError.WorkerError);
            }
            catch (Exception ex)
            {
                logger.FailedUnexpectedly(ex);
                _ = await WriteStatusChange(JobStepStatus.Failed, ex.GetAllMessages(), ex.StackTrace);
                return PerformWorkError.WorkerError;
            }
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

    /// <summary>
    /// Create <typeparamref name="TResult"/> result when the job step is stopped without having the job step task running.
    /// </summary>
    /// <param name="currentState">The current state.</param>
    /// <remarks>
    /// If no kind of result can be derived from the current state null can be returned.
    /// </remarks>
    /// <returns>The result.</returns>
    protected abstract ValueTask<TResult?> CreateCancelledResultFromCurrentState(TState currentState);

    async Task<Result<JobStepError>> WriteStatusChange(JobStepStatus status, IEnumerable<string>? exceptionMessages = null!, string? exceptionStackTrace = null!)
    {
        try
        {
            logger.ChangingStatus(status);
            StatusChanged(status, exceptionMessages, exceptionStackTrace);
            var writeResult = await WriteStateAsync();
            return writeResult.Match(
                _ => Result<JobStepError>.Success(),
                ex => HandleFailedToWriteStatusChange(ex));
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return JobStepError.Unknown;
        }

        JobStepError HandleFailedToWriteStatusChange(Exception ex)
        {
            logger.FailedToWriteState(ex, status);
            return JobStepError.FailedToPersistState;
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
