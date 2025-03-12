// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OneOf.Types;

namespace Cratis.Chronicle.Grains.Workers;

/// <summary>
/// This class should be used as the base class for extending for the creation of long-running, cpu bound, synchronous work.
/// <para>
/// It relies on a configured <see cref="LimitedConcurrencyLevelTaskScheduler"/> that limits concurrent work to some level
/// below the CPU being "fully engaged with work", as to leave enough resources for the Orleans async messaging to get through.
/// </para>
/// </summary>
/// <typeparam name="TRequest">The request type (arguments/parameters) for a long-running piece of work.</typeparam>
/// <typeparam name="TResult">The result/response for a long-running piece of work.</typeparam>
/// <remarks>
/// Based on the work done here: https://github.com/OrleansContrib/Orleans.SyncWork.
/// </remarks>
public abstract class CpuBoundWorker<TRequest, TResult> : Grain, ICpuBoundWorker<TRequest, TResult>
{
    ILogger<ICpuBoundWorker> _logger = null!;
    TaskScheduler? _taskScheduler;
    CpuBoundWorkerStatus _status = CpuBoundWorkerStatus.NotStarted;
    Result<None, Exception> _exception = default(None);
    Task? _task;
    Result<PerformWorkResult<TResult>, WorkerGetResultError> _result = WorkerGetResultError.NotStarted;
    TaskScheduler TaskScheduler => _taskScheduler ??= ServiceProvider.GetService<LimitedConcurrencyLevelTaskScheduler>() ?? TaskScheduler.Default;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger = ServiceProvider.GetService<ILogger<CpuBoundWorker<TRequest, TResult>>>() ?? new NullLogger<CpuBoundWorker<TRequest, TResult>>();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<CpuBoundWorkerStatus> GetWorkStatus() => Task.FromResult(_status);

    /// <inheritdoc />
    public Task<Result<None, Exception>> GetException() => Task.FromResult(_exception);

    /// <inheritdoc/>
    public Task<Result<PerformWorkResult<TResult>, WorkerGetResultError>> GetResult() => Task.FromResult(_result);

    /// <summary>
    /// The method that actually performs the long-running work.
    /// </summary>
    /// <returns>The result of the work.</returns>
    protected abstract Task<PerformWorkResult<TResult>> PerformWork();

    /// <summary>
    /// Method that gets called when the work has succeeded.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnStopped() => Task.CompletedTask;

    /// <summary>
    /// Start long-running work with the provided parameter.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>true if work is started, false if it was already started.</returns>
    protected Task<bool> Start(CancellationToken cancellationToken = default)
    {
        if (_task != null)
        {
            _logger?.TaskHasAlreadyBeenInitialized();
            return Task.FromResult(false);
        }

        _logger?.StartingTask();
        _status = CpuBoundWorkerStatus.Running;
        _task = CreateTask(cancellationToken);

        return Task.FromResult(true);
    }

    /// <summary>
    /// The task creation that fires off the long-running work to the <see cref="LimitedConcurrencyLevelTaskScheduler"/>.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>a <see cref="Task"/> representing the fact that the work has been dispatched.</returns>
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
    Task CreateTask(CancellationToken cancellationToken = default)
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
    {
        return Task.Factory.StartNew(
            async () =>
            {
                _result = WorkerGetResultError.NotFinished;
                if (cancellationToken.IsCancellationRequested)
                {
                    HandleCancellation();
                    return;
                }
                _logger.BeginningWorkForTask();
                PerformWorkResult<TResult> performWorkResult;
                try
                {
                    performWorkResult = await PerformWork();
                }
                catch (Exception e)
                {
                    HandleException(e);
                    return;
                }

                _result = performWorkResult;
                if (performWorkResult.Error == PerformWorkError.Cancelled)
                {
                    HandleCancellation();
                    return;
                }
                if (performWorkResult.HasException)
                {
                    HandleException(performWorkResult.Exception!);
                    return;
                }
                if (!performWorkResult.IsSuccess)
                {
                    HandleError(performWorkResult.Error);
                    return;
                }

                _status = CpuBoundWorkerStatus.Completed;
                _logger.TaskHasCompleted();

                void HandleCancellation()
                {
                    _logger.TaskHasBeenCancelled();
                    _status = CpuBoundWorkerStatus.Stopped;
                    _result = WorkerGetResultError.WorkCancelled;
                    _task = null;
                }

                void HandleException(Exception e)
                {
                    _logger?.TaskHasFailed(e);
                    _exception = e;
                    _status = CpuBoundWorkerStatus.Failed;
                    _result = new PerformWorkResult<TResult>
                    {
                        Error = PerformWorkError.WorkerError,
                        Exception = e
                    };
                    _task = null;
                }
                void HandleError(PerformWorkError e)
                {
                    _logger?.TaskHasFailed(e);
                    _status = CpuBoundWorkerStatus.Failed;
                    _task = null;
                }
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler);
    }
}
