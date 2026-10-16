// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog;

public class when_reconnect_is_requested : given.a_connection_watchdog
{
    async Task Because()
    {
        _watchDog.RequestReconnect();
        _reconnect = () =>
        {
            _reconnected.TrySetResult();
            return Task.CompletedTask;
        };
        _watchDog.Start();
        await _reconnected.Task.WaitAsync(TimeSpan.FromSeconds(10), _time);
        await _cancellationTokenSource.CancelAsync();
    }

    [Fact] void should_notify_session_dropped() => _sessionDroppedCalls.ShouldEqual(1);
    [Fact] void should_reconnect() => _reconnectCalls.ShouldEqual(1);
}
