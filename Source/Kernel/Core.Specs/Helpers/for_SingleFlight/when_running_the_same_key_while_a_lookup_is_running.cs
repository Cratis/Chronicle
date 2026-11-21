// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Helpers.for_SingleFlight;

public class when_running_the_same_key_while_a_lookup_is_running : Specification
{
    SingleFlight<string, int> _flight;
    TaskCompletionSource<int> _gate;
    int _lookups;
    int _first;
    int _second;
    int _inFlightWhileRunning;

    void Establish()
    {
        _flight = new();
        _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        var first = _flight.Run("key", () =>
        {
            Interlocked.Increment(ref _lookups);
            return _gate.Task;
        });
        var second = _flight.Run("key", () =>
        {
            Interlocked.Increment(ref _lookups);
            return Task.FromResult(-1);
        });
        _inFlightWhileRunning = _flight.InFlight;
        _gate.SetResult(42);
        _first = await first;
        _second = await second;
    }

    [Fact] void should_run_one_lookup() => _lookups.ShouldEqual(1);
    [Fact] void should_give_the_first_caller_the_answer() => _first.ShouldEqual(42);
    [Fact] void should_share_the_answer_with_the_second_caller() => _second.ShouldEqual(42);
    [Fact] void should_have_one_lookup_in_flight_while_running() => _inFlightWhileRunning.ShouldEqual(1);
    [Fact] void should_have_nothing_in_flight_afterwards() => _flight.InFlight.ShouldEqual(0);
}
