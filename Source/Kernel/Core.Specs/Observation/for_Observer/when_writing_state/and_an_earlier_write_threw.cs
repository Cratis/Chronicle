// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_an_earlier_write_threw : given.an_observer_with_interceptable_state
{
    readonly InvalidOperationException _failure = new("State storage unavailable");
    Exception _error;

    async Task Establish()
    {
        _interceptedState.OnWrite = () => Task.FromException(_failure);
        _error = await Catch.Exception(() => _observer.SetHandledStats(42UL));
        _interceptedState.OnWrite = () => Task.CompletedTask;
    }

    async Task Because() => await _observer.SetHandledStats(43UL).WaitAsync(TimeSpan.FromSeconds(5));

    [Fact] void should_have_propagated_the_original_exception() => _error.ShouldBeSame(_failure);
    [Fact] void should_release_the_write_lock_for_the_next_write() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_persist_the_next_progress() => _interceptedState.Writes.Last().LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
}
