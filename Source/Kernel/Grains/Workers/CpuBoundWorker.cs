// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.SyncWork;
using Orleans.SyncWork.Enums;
using Orleans.SyncWork.Exceptions;

namespace Aksio.Cratis.Kernel.Grains.Workers;

#pragma warning disable CA1848 // For improved performance, use the LoggerMessage delegates for logging.

/// <summary>
/// This class should be used as the base class for extending for the creation of long running, cpu bound, synchronous work.
/// <para>
/// It relies on a configured <see cref="LimitedConcurrencyLevelTaskScheduler"/> that limits concurrent work to some level
/// below the CPU being "fully engaged with work", as to leave enough resources for the Orleans async messaging to get through.
/// </para>
/// </summary>
/// <typeparam name="TRequest">The request type (arguments/parameters) for a long running piece of work.</typeparam>
/// <typeparam name="TResult">The result/response for a long running piece of work.</typeparam>
/// <remarks>
/// Based on the work done here: https://github.com/OrleansContrib/Orleans.SyncWork.
/// </remarks>
public abstract class CpuBoundWorker<TRequest, TResult> : Grain, ICpuBoundWorker<TRequest, TResult>
{
    ILogger<CpuBoundWorker<TRequest, TResult>>? _logger;
    TaskScheduler? _limitedConcurrencyScheduler;
    SyncWorkStatus _status = SyncWorkStatus.NotStarted;
    TResult? _result;
    Exception? _exception;
    Task? _task;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger = ServiceProvider.GetService<ILogger<CpuBoundWorker<TRequest, TResult>>>() ?? new NullLogger<CpuBoundWorker<TRequest, TResult>>();
        _limitedConcurrencyScheduler = ServiceProvider.GetService<LimitedConcurrencyLevelTaskScheduler>() ?? TaskScheduler.Default;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<SyncWorkStatus> GetWorkStatus()
    {
        if (_status == SyncWorkStatus.NotStarted)
        {
            _logger?.LogError("{Method} was in a status of {WorkStatus}", nameof(GetWorkStatus), SyncWorkStatus.NotStarted);
            DeactivateOnIdle();
            throw new InvalidStateException(_status);
        }

        return Task.FromResult(_status);
    }

    /// <inheritdoc />
    public Task<Exception?> GetException()
    {
        if (_status != SyncWorkStatus.Faulted)
        {
            _logger?.LogError("{Method}: Attempting to retrieve exception from grain when grain not in a faulted state ({_status}).", nameof(GetException), _status);
            DeactivateOnIdle();
            throw new InvalidStateException(_status, SyncWorkStatus.Faulted);
        }

        _task = null;
        DeactivateOnIdle();

        return Task.FromResult(_exception);
    }

    /// <inheritdoc />
    public Task<TResult?> GetResult()
    {
        if (_status != SyncWorkStatus.Completed)
        {
            _logger?.LogError("{Method}: Attempting to retrieve result from grain when grain not in a completed state ({Status}).", nameof(GetResult), _status);
            DeactivateOnIdle();
            throw new InvalidStateException(_status, SyncWorkStatus.Completed);
        }

        _task = null;
        DeactivateOnIdle();

        return Task.FromResult(_result);
    }

    /// <summary>
    /// The method that actually performs the long running work.
    /// </summary>
    /// <param name="request">The request/parameters used for the execution of the method.</param>
    /// <returns>The result of the work.</returns>
    protected abstract Task<TResult> PerformWork(TRequest request);

    /// <summary>
    /// Start long running work with the provided parameter.
    /// </summary>
    /// <param name="request">The parameter containing all necessary information to start the workload.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>true if work is started, false if it was already started.</returns>
    protected Task<bool> Start(TRequest request, CancellationToken cancellationToken = default)
    {
        if (_task != null)
        {
            _logger?.LogTrace("{Method}: Task already initialized upon call.", nameof(Start));
            return Task.FromResult(false);
        }

        _logger?.LogTrace("{Method}: Starting task, set status to running.", nameof(Start));
        _status = SyncWorkStatus.Running;
        _task = CreateTask(request, cancellationToken);

        return Task.FromResult(true);
    }

    /// <summary>
    /// The task creation that fires off the long running work to the <see cref="LimitedConcurrencyLevelTaskScheduler"/>.
    /// </summary>
    /// <param name="request">The request to use for the invoke of the long running work.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>a <see cref="Task"/> representing the fact that the work has been dispatched.</returns>
    Task CreateTask(TRequest request, CancellationToken cancellationToken = default)
    {
        return Task.Factory.StartNew(
            async () =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger?.LogTrace("{Method}: Cancellation requested, exiting.", nameof(CreateTask));
                        return;
                    }

                    _logger?.LogTrace("{Method}: Beginning work for task.", nameof(CreateTask));
                    _result = await PerformWork(request);
                    _exception = default;
                    _status = SyncWorkStatus.Completed;
                    _logger?.LogTrace("{Method}: Completed work for task.", nameof(CreateTask));
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "{Method)}: Exception during task.", nameof(CreateTask));
                    _result = default;
                    _exception = e;
                    _status = SyncWorkStatus.Faulted;
                }
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            _limitedConcurrencyScheduler!);
    }
}
