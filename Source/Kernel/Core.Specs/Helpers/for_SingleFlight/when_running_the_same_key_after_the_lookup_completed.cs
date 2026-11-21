// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Helpers.for_SingleFlight;

public class when_running_the_same_key_after_the_lookup_completed : Specification
{
    SingleFlight<string, int> _flight;
    int _lookups;
    int _first;
    int _second;

    void Establish() => _flight = new();

    async Task Because()
    {
        _first = await _flight.Run("key", () => Task.FromResult(++_lookups));
        _second = await _flight.Run("key", () => Task.FromResult(++_lookups));
    }

    [Fact] void should_run_a_lookup_for_each_call() => _lookups.ShouldEqual(2);
    [Fact] void should_not_reuse_the_earlier_answer() => _second.ShouldEqual(2);
    [Fact] void should_have_given_the_first_answer_to_the_first_call() => _first.ShouldEqual(1);
}
