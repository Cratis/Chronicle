// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog.when_session_drops;

public class and_drop_notification_is_cancelled_without_shutdown : given.a_connection_watchdog
{
    void Establish()
    {
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _sessionDropped = () => throw new OperationCanceledException();
        _reconnect = () =>
        {
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
        await _cancellationTokenSource.CancelAsync();
    }

    [Fact] void should_still_reconnect() => _reconnectCalls.ShouldEqual(1);
    [Fact] void should_log_the_failed_notification() => _logger.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == nameof(ILogger.Log)).GetArguments()[3].ShouldBeOfExactType<OperationCanceledException>();
}
