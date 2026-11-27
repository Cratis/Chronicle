// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_reconnecting;

public class after_two_consecutive_server_losses : given.a_connection_with_several_endpoints
{
    protected override bool WatchdogTicksQuickly => true;

    Subject<ConnectionKeepAlive> _secondSession;
    ChronicleServerAddress _afterFirstLoss;
    int _disconnectedAfterRecovery;
    int _answeredAfterRecovery;

    async Task Because()
    {
        var firstSession = new Subject<ConnectionKeepAlive>();
        _secondSession = new Subject<ConnectionKeepAlive>();
        var thirdSession = new Subject<ConnectionKeepAlive>();
        _streams[_a] = () => Observable.Return(new ConnectionKeepAlive()).Concat(firstSession);
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

        // The server restarts and comes back as another node, which is what the resolver now reports.
        ResolveTo(_b);
        _streams[_b] = () => Observable.Return(new ConnectionKeepAlive()).Concat(_secondSession);
        firstSession.OnCompleted();
        await TickWatchdog();
        _afterFirstLoss = _connection.CurrentServerAddress;

        // And once more, to yet another node, on the very same connection.
        ResolveTo(_c);
        _streams[_c] = () => Observable.Return(new ConnectionKeepAlive()).Concat(thirdSession);
        _secondSession.OnCompleted();
        await TickWatchdog();

        // The replaced session talking after it was replaced must not disturb the connection.
        _disconnectedAfterRecovery = _disconnectedCalls;
        _answeredAfterRecovery = _keepAlivesAnswered;
        _secondSession.OnNext(new ConnectionKeepAlive());
        _secondSession.OnError(Unavailable());
    }

    [Fact] void should_recover_both_times_on_the_same_connection() => _isConnected.ShouldBeTrue();
    [Fact] void should_have_recovered_against_the_first_replacement() => _afterFirstLoss.ShouldEqual(_b);
    [Fact] void should_recover_against_the_second_replacement() => _connection.CurrentServerAddress.ShouldEqual(_c);
    [Fact] void should_resolve_the_candidates_for_every_attempt() => _resolver.Received(3).Resolve(Arg.Any<ChronicleConnectionString>());
    [Fact] void should_create_a_new_call_invoker_for_every_attempt() => _callInvokers.Distinct().Count().ShouldEqual(3);
    [Fact] void should_create_a_new_transport_for_every_attempt() => _handlers.Distinct().Count().ShouldEqual(3);
    [Fact] void should_dispose_the_transports_it_replaced() => _handlers.Take(2).All(_ => _.IsDisposed).ShouldBeTrue();
    [Fact] void should_keep_the_current_transport() => _handlers[2].IsDisposed.ShouldBeFalse();
    [Fact] void should_report_each_loss_once() => _disconnectedAfterRecovery.ShouldEqual(2);
    [Fact] void should_ignore_a_replaced_session() => (_disconnectedCalls, _keepAlivesAnswered).ShouldEqual((_disconnectedAfterRecovery, _answeredAfterRecovery));
}
