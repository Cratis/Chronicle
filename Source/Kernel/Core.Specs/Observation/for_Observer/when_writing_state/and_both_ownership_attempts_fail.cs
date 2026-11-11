// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_writing_state;

public class and_both_ownership_attempts_fail : given.an_observer_with_interceptable_state
{
    ObserverKeyDoesNotMatchDefinition _failure;
    Exception _error;

    void Establish()
    {
        _failure = new(_observerKey, "another-sequence");
        _interceptedState.OnWrite = () => Task.FromException(_failure);
    }

    async Task Because() => _error = await Catch.Exception(() => _observer.SetHandledStats(42UL));

    [Fact] void should_make_exactly_two_write_attempts() => _interceptedState.Writes.Count.ShouldEqual(2);
    [Fact] void should_propagate_the_second_ownership_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_record_a_successful_write() => _storageStats.Writes.ShouldEqual(0);
}
