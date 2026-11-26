// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog.when_session_drops;

public class and_reconnect_is_cancelled_without_shutdown_then_succeeds : given.a_connection_watchdog
{
    void Establish()
    {
        _logger.IsEnabled(LogLevel.Error).Returns(true);
        _reconnect = () =>
        {
            if (_reconnectCalls == 1)
            {
                throw new TaskCanceledException();
            }

            _watchDog.NotifyKeepAlive();
            _reconnected.TrySetResult();
            return Task.CompletedTask;
        };

        _time.Advance(TimeSpan.FromSeconds(6));
    }

    async Task Because()
    {
        _watchDog.Start();
        await _reconnected.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _watchDog.Start();
        await _cancellationTokenSource.CancelAsync();
    }

    [Fact] void should_retry_until_reconnected() => _reconnectCalls.ShouldEqual(2);
    [Fact] void should_notify_session_dropped_once() => _sessionDroppedCalls.ShouldEqual(1);
    [Fact] void should_back_off_one_second_after_first_failure() => _delays[1].ShouldEqual(1000);
    [Fact] void should_keep_the_monitor_running() => _tasks.Received(1).Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
    [Fact] void should_log_the_failed_attempt() => _logger.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == nameof(ILogger.Log)).GetArguments()[3].ShouldBeOfExactType<TaskCanceledException>();
}
