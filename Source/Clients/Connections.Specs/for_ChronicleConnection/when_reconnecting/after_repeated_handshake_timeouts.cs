// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_reconnecting;

public class after_repeated_handshake_timeouts : given.a_connection_with_several_endpoints
{
    protected override bool WatchdogTicksQuickly => true;
    protected override IReadOnlyList<ChronicleServerAddress> Servers => [_a];

    async Task Because()
    {
        var session = new Subject<ConnectionKeepAlive>();
        _streams[_a] = () => Observable.Return(new ConnectionKeepAlive()).Concat(session);
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

        // The server stops answering the handshake: every attempt runs out of its time.
        session.OnCompleted();
        for (var failures = 1; failures <= 3; failures++)
        {
            var timerCreated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _streams[_a] = () =>
            {
                // Arm only once the handshake starts, excluding timers from the earlier compatibility check.
                _time.TimerCreated = () => timerCreated.TrySetResult();
                return Observable.Never<ConnectionKeepAlive>();
            };
            await ReleaseWatchdogDelay();
            await timerCreated.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            _time.Advance(TimeSpan.FromSeconds(ConnectTimeoutSeconds));
        }

        _streams[_a] = Healthy;
        await ReleaseWatchdogDelay();
        await WaitUntil(() => _isConnected);
    }

    [Fact] void should_recover_on_the_same_connection() => _isConnected.ShouldBeTrue();
    [Fact] void should_have_reported_every_handshake_that_timed_out() => HandshakeFailures.ShouldEqual(3);
    [Fact] void should_name_the_endpoint_in_each_report() => _logger.Messages.Where(_ => _.StartsWith("Could not complete the handshake")).All(_ => _.Contains("node-a:35000")).ShouldBeTrue();
    [Fact] void should_resolve_the_candidates_for_every_attempt() => _resolver.Received(5).Resolve(Arg.Any<ChronicleConnectionString>());
    [Fact] void should_create_a_new_transport_for_every_attempt() => _handlers.Distinct().Count().ShouldEqual(5);
    [Fact] void should_dispose_every_transport_it_replaced() => _handlers.Take(4).All(_ => _.IsDisposed).ShouldBeTrue();
    [Fact] void should_keep_the_current_transport() => _handlers[4].IsDisposed.ShouldBeFalse();

    int HandshakeFailures => _logger.Messages.Count(_ => _.StartsWith("Could not complete the handshake"));
}
