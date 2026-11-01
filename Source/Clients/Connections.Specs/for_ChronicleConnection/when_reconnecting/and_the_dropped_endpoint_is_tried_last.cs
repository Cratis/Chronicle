// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_reconnecting;

public class and_the_dropped_endpoint_is_tried_last : given.a_connection_with_several_endpoints
{
    protected override bool WatchdogTicksQuickly => true;

    async Task Because()
    {
        var first = new Subject<ConnectionKeepAlive>();
        _streams[_a] = () => Observable.Return(new ConnectionKeepAlive()).Concat(first);
        _streams[_b] = Healthy;
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        first.OnCompleted();
        await WaitUntil(() => _servedBy.Count == 2 && _isConnected);
    }

    [Fact] void should_try_the_other_endpoint_before_the_dropped_one() => TriedInOrder.ShouldEqual("node-a,node-b");
    [Fact] void should_offer_only_the_other_endpoint_on_the_reconnect() => Offered(1).ShouldEqual("node-b");
    [Fact] void should_not_try_the_dropped_endpoint_again() => _connectsPerServer[_a].ShouldEqual(1);
}
