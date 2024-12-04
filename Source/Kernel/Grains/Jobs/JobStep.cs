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

    /// <inheritdoc/>
    public void Dispose() => _cancellationTokenSource.Dispose();

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Keep the Grain alive forever: Confirmed here: https://github.com/dotnet/orleans/issues/1721#issuecomment-216566448
        DelayDeactivation(TimeSpan.MaxValue);

        await base.OnActivateAsync(cancellationToken);

        _ = this.GetPrimaryKey(out var key);
        var jobStepKey = (JobStepKey)key;

        var grainType = this.GetGrainType();
        state.State.Name = GetType().Name;
        state.State.Id = new(jobStepKey.JobId, JobStepId);
        state.State.Type = grainType;
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepPrepareStartError>> Start(GrainId jobId, object request)
    {
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
            ThisJobStep = GrainFactory.GetGrain(GrainReference.GrainId).AsReference<IJobStep<TRequest, TResult>>();

            StatusChanged(JobStepStatus.Running);
            state.State.Request = request!;
            await WriteStateOrThrow();
            await Start(request, _cancellationTokenSource.Token);
            return Result<JobStepPrepareStartError>.Success();
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
            StatusChanged(JobStepStatus.Paused);
            await WriteStateOrThrow();
            return Result<JobStepError>.Success();
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return JobStepError.Unknown;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepResumeSuccess, JobStepError>> Resume(GrainId grainId)
    {
        using var scope = logger.BeginJobStepScope(State);
        try
        {
            return _running
                ? JobStepResumeSuccess.AlreadyRunning
                : JobStepResumeSuccess.Success;
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return JobStepError.Unknown;
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
            StatusChanged(JobStepStatus.Stopped);
            await WriteStateOrThrow();
            return Result<JobStepError>.Success();
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
        try
        {
            StatusChanged(status);
            await WriteStateOrThrow();
            return Result<JobStepError>.Success();
        }
        catch (Exception ex)
        {
            logger.FailedUnexpectedly(ex);
            return JobStepError.Unknown;
        }
    }

    /// <inheritdoc/>
    public async Task<Result<JobStepError>> ReportFailure(IList<string> errorMessages, string? exceptionStackTrace)
    {
        using var scope = logger.BeginJobStepScope(State);
        try
        {
            StatusChanged(JobStepStatus.Failed, errorMessages, exceptionStackTrace);
            await WriteStateOrThrow();
            await Job.OnStepFailed(JobStepId, JobStepResult.Failed(new(errorMessages, exceptionStackTrace)));
            return Result<JobStepError>.Success();
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
    protected abstract Task<Catch<JobStepResult, PerformWorkError>> PerformStep(TRequest request, CancellationToken cancellationToken);

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult, PerformWorkError>> PerformWork(TRequest request)
    {
        using var scope = logger.BeginJobStepScope(State);
        try
        {
            await ThisJobStep.ReportStatusChange(JobStepStatus.Running);
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                return PerformWorkError.Cancelled;
            }

            var result = await PerformStep(request, _cancellationTokenSource.Token);
            return await result.Match(HandleSuccessfulPerform, HandleError, HandleException);
        }
        catch (Exception ex)
        {
            return await HandleException(ex);
        }
        finally
        {
            DeactivateOnIdle();
        }

        async Task<Catch<JobStepResult, PerformWorkError>> HandleSuccessfulPerform(JobStepResult result)
        {
            // TODO: Handle this appropriately when Job returns monads.
            try
            {
                if (!result.TryGetError(out var error))
                {
                    await Job.OnStepSucceeded(JobStepId, result);
                    await ThisJobStep.ReportStatusChange(JobStepStatus.Succeeded);
                }
                else
                {
                    var reportErrorResult = await ReportError(error.ErrorMessages.ToList(), error.ExceptionStackTrace ?? string.Empty);
                    if (!reportErrorResult.IsSuccess)
                    {
                        return reportErrorResult.AsT1;
                    }
                }
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        async Task<Catch<JobStepResult, PerformWorkError>> HandleError(PerformWorkError error)
        {
            IList<string> messages = error switch
            {
                PerformWorkError.Cancelled => ["Job step task was cancelled"],
                _ => ["An unknown error occurred"]
            };
            var reportErrorResult = await ReportError(messages, string.Empty);
            return reportErrorResult.Match<Catch<JobStepResult, PerformWorkError>>(_ => error, ex => ex);
        }

        async Task<Catch<JobStepResult, PerformWorkError>> HandleException(Exception ex)
        {
            logger.HandleUnexpectedPerformJobStepFailure(ex);
            var reportErrorResult = await ReportError(ex.GetAllMessages(), ex.StackTrace ?? string.Empty);
            return reportErrorResult.Match(
                _ => ex,
                error => error);
        }

        async Task<Catch> ReportError(IEnumerable<string> errorMessages, string exceptionStackTrace)
        {
            try
            {
                logger.ReportFailure();
                var reportFailureResult = await ThisJobStep.ReportFailure(errorMessages.ToList(), exceptionStackTrace);
                return await reportFailureResult.Match(
                    _ => Task.FromResult(Catch.Success()),
                    _ => Task.FromResult(Catch.Success())); // Here we can later choose what to do.
            }
            catch (Exception ex)
            {
                logger.FailedToReportFailure(ex);
                return ex;
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
    /// Saves the current grain state or throws the error if writing state failed.
    /// </summary>
    /// <returns>A task representing the asynchronous action.</returns>
    protected async Task WriteStateOrThrow()
    {
        var writeResult = await WriteStateAsync();
        writeResult.RethrowError();
    }

    void StatusChanged(JobStepStatus status, IEnumerable<string>? exceptionMessages = null!, string? exceptionStackTrace = null!)
    {
        state.State.StatusChanges.Add(new JobStepStatusChanged
        {
            Status = status,
            Occurred = DateTimeOffset.UtcNow,
            ExceptionMessages = exceptionMessages ?? [],
            ExceptionStackTrace = exceptionStackTrace ?? string.Empty
        });
        state.State.Status = status;
    }
}
