// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_states_emit_repeatedly : given.an_observed_state_stream
{
    async Task Because()
    {
        for (var i = 0; i < 5; i++)
        {
            await Emit(("first", Concepts.Observation.ObserverRunningState.Active, (ulong)i));
            await EmittedCount(i + 1);
        }
    }

    [Fact] void should_not_read_definitions_per_state_emission() => DefinitionReads.ShouldEqual(0);
}
