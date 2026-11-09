// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting_concurrently;

public class and_callers_arrive_while_a_connect_is_in_flight : given.a_connection_with_a_connect_in_flight
{
    IServices[] _results;

    async Task Because()
    {
        var starter = StartCaller();
        await AttemptHasStarted();
        var callers = new[] { starter, StartCaller(), StartCaller(), StartSyncCaller() };
        await HandshakeIsPending();
        _handshake.SetResult();
        await _connectedEntered.Task;
        _connectedGate.SetResult();
        _results = await Task.WhenAll(callers);
    }

    [Fact] void should_resolve_the_servers_once() => _resolves.ShouldEqual(1);
    [Fact] void should_open_one_keep_alive_stream() => _connectsPerServer[_a].ShouldEqual(1);
    [Fact] void should_run_the_connected_handlers_once() => _connectedCalls.ShouldEqual(1);
    [Fact] void should_give_every_caller_the_same_services() => _results.All(_ => ReferenceEquals(_, _results[0])).ShouldBeTrue();
    [Fact] void should_be_connected() => _isConnected.ShouldBeTrue();
}
