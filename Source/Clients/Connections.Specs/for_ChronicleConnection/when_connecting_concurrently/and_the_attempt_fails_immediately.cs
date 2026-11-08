// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting_concurrently;

public class and_the_attempt_fails_immediately : given.a_connection_with_a_connect_in_flight
{
    readonly InvalidOperationException _failure = new("the servers could not be resolved");
    Exception _starterError;
    Exception _joinerError;

    void Establish() => _resolveGate = NewGate();

    async Task Because()
    {
        var starter = StartCaller();
        await AttemptHasStarted();
        var joiner = StartCaller();
        _resolveGate.SetException(_failure);
        _starterError = await Catch.Exception(() => starter);
        _joinerError = await Catch.Exception(() => joiner);
    }

    [Fact] void should_fail_the_starter() => _starterError.ShouldNotBeNull();
    [Fact] void should_fail_the_joiner_instead_of_reporting_success() => _joinerError.ShouldNotBeNull();
    [Fact] void should_give_the_joiner_the_failure_of_the_attempt() => _joinerError.ShouldBeSame(_starterError);
    [Fact] void should_have_tried_once() => _resolves.ShouldEqual(1);
}
