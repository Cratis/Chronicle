// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_a_definition_is_added_without_state : given.an_observed_state_stream
{
    async Task Because()
    {
        await Emit(("first", Concepts.Observation.ObserverRunningState.Active, 1), ("second", Concepts.Observation.ObserverRunningState.Active, 2));
        await EmittedCount(1);
        EmitDefinitions("first", "second", "third");
        await EmittedCount(2);
    }

    [Fact] void should_include_the_new_definition() => _emitted[1].Length.ShouldEqual(3);
    [Fact] void should_use_the_empty_state() => _emitted[1].Single(_ => _.Id == "third").RunningState.ShouldEqual(ObserverRunningState.Unknown);
}
