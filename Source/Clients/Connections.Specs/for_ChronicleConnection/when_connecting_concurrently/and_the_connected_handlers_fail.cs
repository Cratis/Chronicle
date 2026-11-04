// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting_concurrently;

public class and_the_connected_handlers_fail : given.a_connection_with_a_connect_in_flight
{
    readonly AggregateException _failure = new(new InvalidOperationException("handler failed"));
    Exception _cancelledJoiner;
    Exception _starterError;
    Exception _firstJoinerError;
    Exception _secondJoinerError;
    int _connectedCallsFirstAttempt;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var starter = StartCaller();
        await AttemptHasStarted();
        var cancelled = StartCaller(cancellation.Token);
        var first = StartCaller();
        var second = StartCaller();
        await HandshakeIsPending();
        await cancellation.CancelAsync();
        _cancelledJoiner = await Catch.Exception(() => cancelled);
        _connectedFailure = _failure;
        _handshake.SetResult();
        await _connectedEntered.Task;
        _connectedGate.SetResult();
        _starterError = await Catch.Exception(() => starter);
        _firstJoinerError = await Catch.Exception(() => first);
        _secondJoinerError = await Catch.Exception(() => second);
        _connectedCallsFirstAttempt = _connectedCalls;
        await Catch.Exception(_connection.Reconnect);
    }

    [Fact] void should_cancel_only_the_cancelled_joiner() => _cancelledJoiner.ShouldBeAssignableFrom<OperationCanceledException>();
    [Fact] void should_fail_the_starter_with_the_failure() => _starterError.ShouldBeSame(_failure);
    [Fact] void should_fail_the_first_joiner_with_the_same_failure() => _firstJoinerError.ShouldBeSame(_failure);
    [Fact] void should_fail_the_second_joiner_with_the_same_failure() => _secondJoinerError.ShouldBeSame(_failure);
    [Fact] void should_run_the_connected_handlers_once_for_the_attempt() => _connectedCallsFirstAttempt.ShouldEqual(1);
    [Fact] void should_start_a_new_attempt_for_the_next_caller() => _resolves.ShouldEqual(2);
}
