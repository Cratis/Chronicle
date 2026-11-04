// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting_concurrently;

public class and_a_joiners_deadline_expires_while_connected_handlers_run : given.a_connection_with_a_connect_in_flight
{
    Exception _joinerError;

    async Task Because()
    {
        var starter = StartCaller();
        await AttemptHasStarted();
        var joiner = StartCaller();
        await HandshakeIsPending();

        // The joiner's transport deadline and the handshake wait are both pending before the handshake completes.
        await _time.WhenNextTimer();
        await _time.WhenNextTimer();
        _handshake.SetResult();
        await _connectedEntered.Task;

        // The joiner's handler deadline starts once the transport is established.
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(ConnectTimeoutSeconds));
        _joinerError = await Catch.Exception(() => joiner);
        _connectedGate.SetResult();
        await starter;
    }

    [Fact] void should_time_out_the_joiner() => _joinerError.ShouldBeAssignableFrom<ConnectionTimedOut>();
    [Fact] void should_let_the_attempt_finish_for_the_starter() => _isConnected.ShouldBeTrue();
    [Fact] void should_run_the_connected_handlers_once() => _connectedCalls.ShouldEqual(1);
}
