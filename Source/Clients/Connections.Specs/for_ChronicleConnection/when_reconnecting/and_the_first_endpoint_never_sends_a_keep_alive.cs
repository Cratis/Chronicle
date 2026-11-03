// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_reconnecting;

public class and_the_first_endpoint_never_sends_a_keep_alive : given.a_connection_with_several_endpoints
{
    DateTimeOffset _started;

    async Task Because()
    {
        _started = _time.GetUtcNow();
        _streams[_b] = Healthy;
        var connecting = StartConnecting();
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(5));
        await connecting.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    [Fact] void should_be_connected() => _isConnected.ShouldBeTrue();
    [Fact] void should_try_the_first_endpoint_then_the_second() => TriedInOrder.ShouldEqual("node-a,node-b");
    [Fact] void should_offer_both_endpoints_first() => Offered(0).ShouldEqual("node-a,node-b");
    [Fact] void should_offer_only_the_untried_endpoint_next() => Offered(1).ShouldEqual("node-b");
    [Fact] void should_not_wait_the_connect_timeout() => (_time.GetUtcNow() - _started).ShouldBeLessThan(TimeSpan.FromSeconds(ConnectTimeoutSeconds));
}
