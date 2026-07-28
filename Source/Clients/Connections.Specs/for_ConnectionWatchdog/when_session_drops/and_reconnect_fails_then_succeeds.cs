// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog.when_session_drops;

public class and_reconnect_fails_then_succeeds : given.a_connection_watchdog
{
    void Establish()
    {
        _reconnect = () =>
        {
            if (_reconnectCalls < 3)
            {
                throw new SimulatedFailure();
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
        await _cancellationTokenSource.CancelAsync();
    }

    [Fact] void should_retry_until_reconnected() => _reconnectCalls.ShouldEqual(3);
    [Fact] void should_notify_session_dropped_once() => _sessionDroppedCalls.ShouldEqual(1);
    [Fact] void should_back_off_one_second_after_first_failure() => _delays[1].ShouldEqual(1000);
    [Fact] void should_back_off_two_seconds_after_second_failure() => _delays[2].ShouldEqual(2000);
}
