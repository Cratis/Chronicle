// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting_concurrently;

public class and_a_caller_arrives_after_the_transport_is_established : given.a_connection_with_a_connect_in_flight
{
    int _connectedHandlerRuns;

    protected override async Task OnLifecycleConnected()
    {
        // A disconnect begins while the handlers run, so the generation this attempt produced is already gone.
        Interlocked.Increment(ref _connectedCalls);
        _connectedEntered.TrySetResult();
        await _connectedGate.Task;
        _isConnected = false;
        _connectedHandlerRuns++;
    }

    async Task Because()
    {
        var starter = StartCaller();
        await AttemptHasStarted();
        await HandshakeIsPending();
        _handshake.SetResult();
        await _connectedEntered.Task;

        var lateCaller = StartCaller();
        _connectedGate.SetResult();
        await starter;
        await lateCaller;
    }

    [Fact] void should_start_a_fresh_attempt_for_the_late_caller() => _resolves.ShouldEqual(2);
    [Fact] void should_run_the_connected_handlers_for_both_attempts() => _connectedHandlerRuns.ShouldEqual(2);
}
