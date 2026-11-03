// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_reconnecting;

public class and_the_watchdog_drops_a_silent_endpoint : given.a_connection_with_several_endpoints
{
    protected override bool WatchdogTicksQuickly => true;

    async Task Because()
    {
        // The first endpoint heartbeats once and stays open, but goes silent.
        _streams[_a] = () => Observable.Return(new ConnectionKeepAlive()).Concat(Observable.Never<ConnectionKeepAlive>());
        _streams[_b] = Healthy;
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

        // Time passes with no keep-alive, so the watchdog declares the session dropped.
        _time.Advance(TimeSpan.FromMinutes(5));
        await TickWatchdog();
        await WaitUntil(() => _servedBy.Count == 2 && _isConnected);
    }

    [Fact] void should_try_the_other_endpoint_before_the_silent_one() => TriedInOrder.ShouldEqual("node-a,node-b");
    [Fact] void should_offer_only_the_other_endpoint_on_the_reconnect() => Offered(1).ShouldEqual("node-b");
}
