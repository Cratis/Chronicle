// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_reconnecting;

public class after_a_prolonged_single_endpoint_outage : given.a_connection_with_several_endpoints
{
    protected override IReadOnlyList<ChronicleServerAddress> Servers => [_a];
    protected override bool WatchdogTicksQuickly => true;

    async Task Because()
    {
        var session = new Subject<ConnectionKeepAlive>();
        _streams[_a] = () => Observable.Return(new ConnectionKeepAlive()).Concat(session);
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        _streams[_a] = () => Observable.Throw<ConnectionKeepAlive>(new RpcException(new Status(StatusCode.Unavailable, "synthetic outage")));
        session.OnCompleted();

        for (var attempt = 0; attempt < 4; attempt++)
        {
            _time.Advance(TimeSpan.FromMinutes(5));
            await TickWatchdog();
        }

        _streams[_a] = Healthy;
        _time.Advance(TimeSpan.FromMinutes(5));
        await TickWatchdog();
    }

    [Fact] void should_recover_without_replacing_the_connection() => _isConnected.ShouldBeTrue();
    [Fact] void should_have_retried_throughout_the_outage() => _connectsPerServer[_a].ShouldEqual(6);
    [Fact] void should_resolve_the_endpoint_on_every_attempt() => _resolver.Received(6).Resolve(Arg.Any<ChronicleConnectionString>());
    [Fact] void should_create_a_new_call_invoker_on_every_attempt() => _callInvokers.Distinct().Count().ShouldEqual(6);
}
