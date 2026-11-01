// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_reconnecting;

public class with_a_single_endpoint : given.a_connection_with_several_endpoints
{
    Task _connecting;
    bool _completedAfterTheHandshakeCap;

    protected override IReadOnlyList<ChronicleServerAddress> Servers => [_a];

    async Task Because()
    {
        _connecting = StartConnecting();
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(5));
        await Task.Delay(100);
        _completedAfterTheHandshakeCap = _connecting.IsCompleted;
        _time.Advance(TimeSpan.FromSeconds(ConnectTimeoutSeconds - 5));
        await _connecting.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    [Fact] void should_keep_waiting_past_the_handshake_cap() => _completedAfterTheHandshakeCap.ShouldBeFalse();
    [Fact] void should_wait_the_full_connect_timeout() => _time.TimerDueTimes.Single().ShouldEqual(TimeSpan.FromSeconds(ConnectTimeoutSeconds));
    [Fact] void should_not_be_connected() => _isConnected.ShouldBeFalse();
}
