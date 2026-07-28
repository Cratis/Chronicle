// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog.when_session_drops;

public class and_drop_notification_fails : given.a_connection_watchdog
{
    void Establish()
    {
        _sessionDropped = () => throw new SimulatedFailure();
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
}
