// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_a_failed_connected_handler_is_followed_by_stream_completion : given.a_connection_with_several_endpoints
{
    readonly InvalidOperationException _failure = new("handler failed");
    Exception _error;
    int _connectedCalls;

    protected override bool WatchdogTicksQuickly => true;
    protected override IReadOnlyList<ChronicleServerAddress> Servers => [_a];

    void Establish()
    {
        _lifecycle.Connected().Returns(_ =>
        {
            _isConnected = true;
            return ++_connectedCalls == 1 ? Task.FromException(_failure) : Task.CompletedTask;
        });
        _isConnected = false;
    }

    async Task Because()
    {
        var first = new Subject<ConnectionKeepAlive>();
        var streams = 0;
        _streams[_a] = () => ++streams == 1 ? Observable.Return(new ConnectionKeepAlive()).Concat(first) : Healthy();
        _error = await Catch.Exception(() => StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System));
        first.OnCompleted();
        await TickWatchdog();
    }

    [Fact] void should_surface_the_original_handler_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_reconnect_without_a_foreground_service_call() => _servedBy.Count.ShouldEqual(2);
    [Fact] void should_report_the_disconnect_once() => _disconnectedCalls.ShouldEqual(1);
    [Fact] void should_be_connected() => _isConnected.ShouldBeTrue();
    [Fact] void should_start_only_one_monitor() => _tasks.Received(1).Run(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>());
}
