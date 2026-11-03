// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_the_keep_alive_stream;

public class ends_after_connecting : given.a_connection_with_several_endpoints
{
    DateTimeOffset _started;

    protected override bool WatchdogTicksQuickly => true;
    protected override IReadOnlyList<ChronicleServerAddress> Servers => [_a];

    async Task Because()
    {
        _started = _time.GetUtcNow();
        var first = new Subject<ConnectionKeepAlive>();
        var streams = 0;
        _streams[_a] = () => ++streams == 1 ? Observable.Return(new ConnectionKeepAlive()).Concat(first) : Healthy();
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        first.OnCompleted();
        await TickWatchdog();
        await WaitUntil(() => _servedBy.Count == 2 && _isConnected);
    }

    [Fact] void should_reconnect() => _isConnected.ShouldBeTrue();
    [Fact] void should_report_the_disconnect_once() => _disconnectedCalls.ShouldEqual(1);
    [Fact] void should_reconnect_without_waiting_for_the_keep_alive_to_go_stale() => (_time.GetUtcNow() - _started).ShouldEqual(TimeSpan.Zero);
}
