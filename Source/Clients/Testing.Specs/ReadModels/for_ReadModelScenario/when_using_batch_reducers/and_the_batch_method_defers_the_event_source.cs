// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_using_batch_reducers;

public class and_the_batch_method_defers_the_event_source : given.a_batch_reducer_scenario
{
    Exception _error;

    async Task Because()
    {
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new SeatCounted(), new SeatCountPostponed());
        _error = Catch.Exception(() => _ = _scenario.Instance);
    }

    [Fact] void should_surface_the_deferral() => _error.ShouldBeOfExactType<ReducerDeferred>();
    [Fact] void should_carry_the_delay() => ((ReducerDeferred)_error).RetryAfter.ShouldEqual(TimeSpan.FromSeconds(30));
}
