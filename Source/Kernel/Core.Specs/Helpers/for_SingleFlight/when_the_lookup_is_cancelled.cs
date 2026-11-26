// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Helpers.for_SingleFlight;

public class when_the_lookup_is_cancelled : Specification
{
    SingleFlight<string, int> _flight;
    TaskCompletionSource<int> _gate;
    CancellationTokenSource _cancellation;
    Task<int> _leader;
    Task<int> _joiner;
    int _afterwards;

    void Establish()
    {
        _flight = new();
        _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _cancellation = new();
    }

    async Task Because()
    {
        _leader = _flight.Run("key", () => _gate.Task.WaitAsync(_cancellation.Token));
        _joiner = _flight.Run("key", () => Task.FromResult(-1));
        await _cancellation.CancelAsync();
        await Catch.Exception(async () => await _leader);
        await Catch.Exception(async () => await _joiner);
        _afterwards = await _flight.Run("key", () => Task.FromResult(7));
    }

    [Fact] void should_cancel_the_leader() => _leader.IsCanceled.ShouldBeTrue();
    [Fact] void should_cancel_the_joiner() => _joiner.IsCanceled.ShouldBeTrue();
    [Fact] void should_run_a_new_lookup_for_the_next_caller() => _afterwards.ShouldEqual(7);
    [Fact] void should_have_nothing_in_flight() => _flight.InFlight.ShouldEqual(0);
}
