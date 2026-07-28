// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Tasks;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog.given;

public class a_connection_watchdog : Specification
{
    readonly Lock _delaysLock = new();
    protected ConnectionWatchdog _watchDog;
    protected ITaskFactory _tasks;
    protected ILogger<ConnectionWatchdog> _logger;
    protected ControllableTimeProvider _time;
    protected CancellationTokenSource _cancellationTokenSource;
    protected List<int> _delays;
    protected int _sessionDroppedCalls;
    protected int _reconnectCalls;
    protected Func<Task> _sessionDropped;
    protected Func<Task> _reconnect;
    protected TaskCompletionSource _reconnected;

    void Establish()
    {
        _tasks = Substitute.For<ITaskFactory>();
        _logger = Substitute.For<ILogger<ConnectionWatchdog>>();
        _time = new ControllableTimeProvider();
        _cancellationTokenSource = new CancellationTokenSource();
        _delays = [];
        _sessionDropped = () => Task.CompletedTask;
        _reconnect = () => Task.CompletedTask;
        _reconnected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _tasks.Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.Run(callInfo.Arg<Func<Task>>()));
        _tasks.Delay(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                lock (_delaysLock)
                {
                    _delays.Add(callInfo.Arg<int>());
                }

                return Task.Delay(1, callInfo.Arg<CancellationToken>());
            });

        _watchDog = new ConnectionWatchdog(
            _tasks,
            () =>
            {
                Interlocked.Increment(ref _sessionDroppedCalls);
                return _sessionDropped();
            },
            () =>
            {
                Interlocked.Increment(ref _reconnectCalls);
                return _reconnect();
            },
            _logger,
            _cancellationTokenSource.Token,
            _time);
    }

    void Destroy() => _cancellationTokenSource.Cancel();
}
