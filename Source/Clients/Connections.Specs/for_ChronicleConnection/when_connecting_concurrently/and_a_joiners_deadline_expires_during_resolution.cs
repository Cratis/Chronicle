// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting_concurrently;

public class and_a_joiners_deadline_expires_during_resolution : given.a_connection_with_a_connect_in_flight
{
    Exception _joinerError;
    int _timersAfterJoining;

    void Establish() => _resolveGate = NewGate();

    async Task Because()
    {
        var starter = StartCaller();
        await AttemptHasStarted();
        var joiner = StartCaller();
        _timersAfterJoining = _time.TimerDueTimes.Count;
        _time.Advance(TimeSpan.FromSeconds(ConnectTimeoutSeconds));
        _joinerError = await Catch.Exception(() => joiner);
        _resolveGate.SetResult();
        await HandshakeIsPending();
        _handshake.SetResult();
        await _connectedEntered.Task;
        _connectedGate.SetResult();
        await starter;
    }

    [Fact] void should_only_have_the_joiners_deadline_running() => _timersAfterJoining.ShouldEqual(1);
    [Fact] void should_time_out_the_joiner() => _joinerError.ShouldBeAssignableFrom<ConnectionTimedOut>();
    [Fact] void should_let_the_attempt_finish_for_the_starter() => _isConnected.ShouldBeTrue();
    [Fact] void should_resolve_the_servers_once() => _resolves.ShouldEqual(1);
    [Fact] void should_run_the_connected_handlers_once() => _connectedCalls.ShouldEqual(1);
}
