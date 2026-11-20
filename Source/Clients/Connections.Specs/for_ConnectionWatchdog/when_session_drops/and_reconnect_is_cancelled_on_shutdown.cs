// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog.when_session_drops;

public class and_reconnect_is_cancelled_on_shutdown : given.a_connection_watchdog
{
    TaskCompletionSource<Task> _monitorStarted;
    Task _monitor;

    void Establish()
    {
        _monitorStarted = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        _tasks.Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var monitor = Task.Run(callInfo.Arg<Func<Task>>());
                _monitorStarted.TrySetResult(monitor);
                return monitor;
            });
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _reconnect = async () =>
        {
            await _cancellationTokenSource.CancelAsync();
            throw new OperationCanceledException(_cancellationTokenSource.Token);
        };

        _time.Advance(TimeSpan.FromSeconds(6));
    }

    async Task Because()
    {
        _watchDog.Start();
        _monitor = await _monitorStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _monitor.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_stop_the_monitor() => _monitor.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_not_retry() => _reconnectCalls.ShouldEqual(1);
    [Fact] void should_not_log_a_failure() => _logger.ReceivedCalls().Where(_ => _.GetMethodInfo().Name == nameof(ILogger.Log) && _.GetArguments()[0] is LogLevel.Error or LogLevel.Warning).ShouldBeEmpty();
}
