// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ConnectionWatchdog.when_session_drops;

public class and_reconnect_keeps_failing : given.a_connection_watchdog
{
    void Establish()
    {
        _reconnect = () =>
        {
            if (_reconnectCalls < 8)
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

    [Fact] void should_keep_retrying_until_reconnected() => _reconnectCalls.ShouldEqual(8);
    [Fact] void should_cap_backoff_at_thirty_seconds() => _delays[6].ShouldEqual(30000);
    [Fact] void should_stay_at_capped_backoff_for_further_failures() => _delays[7].ShouldEqual(30000);
}
