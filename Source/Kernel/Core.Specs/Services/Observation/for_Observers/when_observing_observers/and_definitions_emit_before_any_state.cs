// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_definitions_emit_before_any_state : given.an_observed_state_stream
{
    int _emissionsBeforeState;

    async Task Because()
    {
        EmitDefinitions("third");
        _emissionsBeforeState = _emitted.Count;
        await Emit(("third", Concepts.Observation.ObserverRunningState.Active, 1));
        await EmittedCount(1);
    }

    [Fact] void should_wait_for_state() => _emissionsBeforeState.ShouldEqual(0);
    [Fact] void should_use_the_latest_definitions() => _emitted[0].Single().Id.ShouldEqual("third");
}
