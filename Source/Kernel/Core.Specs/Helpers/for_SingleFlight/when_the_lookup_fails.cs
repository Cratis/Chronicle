// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Helpers.for_SingleFlight;

public class when_the_lookup_fails : Specification
{
    SingleFlight<string, int> _flight;
    TaskCompletionSource<int> _gate;
    Exception _leaderException;
    Exception _joinerException;
    int _afterwards;

    void Establish()
    {
        _flight = new();
        _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        var leader = _flight.Run("key", () => _gate.Task);
        var joiner = _flight.Run("key", () => Task.FromResult(-1));
        _gate.SetException(new InvalidOperationException("lookup failed"));
        _leaderException = await Catch.Exception(async () => await leader);
        _joinerException = await Catch.Exception(async () => await joiner);
        _afterwards = await _flight.Run("key", () => Task.FromResult(7));
    }

    [Fact] void should_fail_the_leader() => _leaderException.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_fail_the_joiner() => _joinerException.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_run_a_new_lookup_for_the_next_caller() => _afterwards.ShouldEqual(7);
    [Fact] void should_have_nothing_in_flight() => _flight.InFlight.ShouldEqual(0);
}
