// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting_concurrently;

public class and_the_caller_that_started_it_is_cancelled : given.a_connection_with_a_connect_in_flight
{
    Exception _starterError;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var starter = StartCaller(cancellation.Token);
        await AttemptHasStarted();
        var joiner = StartCaller();
        await HandshakeIsPending();
        await cancellation.CancelAsync();
        _starterError = await Catch.Exception(() => starter);
        _handshake.SetResult();
        await _connectedEntered.Task;
        _connectedGate.SetResult();
        await joiner;
    }

    [Fact] void should_cancel_the_starter() => _starterError.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_let_the_attempt_finish_for_the_joiner() => _isConnected.ShouldBeTrue();
    [Fact] void should_resolve_the_servers_once() => _resolves.ShouldEqual(1);
    [Fact] void should_run_the_connected_handlers_once() => _connectedCalls.ShouldEqual(1);
}
