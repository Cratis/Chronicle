// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OneOf.Types;

namespace Cratis.Chronicle.Workers;

/// <summary>
/// This class should be used as the base class for extending for the creation of long-running, cpu bound, synchronous work.
/// </summary>
/// <typeparam name="TRequest">The request type (arguments/parameters) for a long-running piece of work.</typeparam>
/// <typeparam name="TResult">The result/response for a long-running piece of work.</typeparam>
public abstract class GrainWithBackgroundTask<TRequest, TResult> : Grain, IGrainWithBackgroundTask<TRequest, TResult>
{
    ILogger<IGrainWithBackgroundTask> _logger = null!;
    GrainWithBackgroundTaskStatus _status = GrainWithBackgroundTaskStatus.NotStarted;
    Result<None, Exception> _exception = default(None);
    Task? _task;
    Result<PerformWorkResult<TResult>, WorkerGetResultError> _result = WorkerGetResultError.NotStarted;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger = ServiceProvider.GetService<ILogger<GrainWithBackgroundTask<TRequest, TResult>>>() ?? new NullLogger<GrainWithBackgroundTask<TRequest, TResult>>();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<GrainWithBackgroundTaskStatus> GetWorkStatus() => Task.FromResult(_status);

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
        _status = GrainWithBackgroundTaskStatus.Running;
        _task = CreateTask(cancellationToken);

        return Task.FromResult(true);
    }

    /// <summary>
    /// The task creation that fires off.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>a <see cref="Task"/> representing the fact that the work has been dispatched.</returns>
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
    Task CreateTask(CancellationToken cancellationToken = default)
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
    {
        return Task.Run(
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

                _status = GrainWithBackgroundTaskStatus.Completed;
                _logger.TaskHasCompleted();

                void HandleCancellation()
                {
                    _logger.TaskHasBeenCancelled();
                    _status = GrainWithBackgroundTaskStatus.Stopped;
                    _result = WorkerGetResultError.WorkCancelled;
                    _task = null;
                }

                void HandleException(Exception e)
                {
                    _logger?.TaskHasFailed(e);
                    _exception = e;
                    _status = GrainWithBackgroundTaskStatus.Failed;
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
                    _status = GrainWithBackgroundTaskStatus.Failed;
                    _task = null;
                }
            });
    }
}
