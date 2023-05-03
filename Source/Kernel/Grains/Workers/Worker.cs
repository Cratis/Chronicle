// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Workers;

/// <summary>
/// Represents an implementation of <see cref="IWorker{TRequest, TResponse}"/>.
/// </summary>
/// <typeparam name="TRequest">Type of request.</typeparam>
/// <typeparam name="TResult">Type of response.</typeparam>
public abstract class Worker<TRequest, TResult> : Grain, IWorker<TRequest, TResult>
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ILogger _logger;
    readonly string _name;
    WorkerStatus _status = WorkerStatus.Idle;
    Exception? _exception;
    TResult? _result;
    IDisposable? _startTimer;
    ExecutionContext _executionContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker{TRequest, TResult}"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="logger">Logger for logging.</param>
    protected Worker(IExecutionContextManager executionContextManager, ILogger logger)
    {
        _executionContextManager = executionContextManager;
        _executionContext = _executionContextManager.Current;
        _logger = logger;
        _name = GetType().Name;
    }

    /// <inheritdoc/>
    public Task<Exception> GetException() => Task.FromResult(_exception!);

    /// <inheritdoc/>
    public Task<TResult> GetResult() => Task.FromResult(_result!);

    /// <inheritdoc/>
    public Task<WorkerStatus> GetStatus() => Task.FromResult(_status);

    /// <inheritdoc/>
    public Task Start(TRequest request)
    {
        _status = WorkerStatus.Working;

        _logger.WorkerStarted(_name);
        _executionContext = _executionContextManager.Current;
        _startTimer = RegisterTimer(HandlePerformWork, request, TimeSpan.Zero, TimeSpan.MaxValue);

        return Task.CompletedTask;
    }

    /// <summary>
    /// The actual work to perform.
    /// </summary>
    /// <param name="request">Request.</param>
    /// <returns>Result.</returns>
    protected abstract Task<TResult> PerformWork(TRequest request);

    async Task HandlePerformWork(object state)
    {
        _startTimer?.Dispose();
        _startTimer = null!;

        try
        {
            _status = WorkerStatus.Working;
            _executionContextManager.Set(_executionContext);
            _result = await PerformWork((TRequest)state);
            _logger.WorkerCompleted(_name);
            _status = WorkerStatus.Completed;
        }
        catch (Exception ex)
        {
            _exception = ex;
            _logger.WorkerFailed(_name, ex);
            _status = WorkerStatus.Failed;
        }
    }
}
