// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog;

public class when_keep_alive_is_fresh : given.a_connection_watchdog
{
    async Task Because()
    {
        _watchDog.Start();
        await Task.Delay(100);
        await _cancellationTokenSource.CancelAsync();
    }

    [Fact] void should_not_notify_session_dropped() => _sessionDroppedCalls.ShouldEqual(0);
    [Fact] void should_not_reconnect() => _reconnectCalls.ShouldEqual(0);
}
