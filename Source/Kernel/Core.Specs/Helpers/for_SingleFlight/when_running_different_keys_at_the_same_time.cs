// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Helpers.for_SingleFlight;

public class when_running_different_keys_at_the_same_time : Specification
{
    SingleFlight<string, string> _flight;
    TaskCompletionSource<string> _gate;
    int _lookups;
    string _first;
    string _second;

    void Establish()
    {
        _flight = new();
        _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        var first = _flight.Run("one", () =>
        {
            Interlocked.Increment(ref _lookups);
            return _gate.Task.ContinueWith(_ => "one-answer", TaskScheduler.Default);
        });
        var second = _flight.Run("two", () =>
        {
            Interlocked.Increment(ref _lookups);
            return _gate.Task.ContinueWith(_ => "two-answer", TaskScheduler.Default);
        });
        _gate.SetResult(string.Empty);
        _first = await first;
        _second = await second;
    }

    [Fact] void should_run_a_lookup_for_each_key() => _lookups.ShouldEqual(2);
    [Fact] void should_answer_the_first_key() => _first.ShouldEqual("one-answer");
    [Fact] void should_answer_the_second_key() => _second.ShouldEqual("two-answer");
}
