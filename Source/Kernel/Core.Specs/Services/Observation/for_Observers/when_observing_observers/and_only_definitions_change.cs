// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_observing_observers;

public class and_only_definitions_change : given.an_observed_state_stream
{
    int _queriesAfterInitialState;

    async Task Because()
    {
        await Emit(("first", Concepts.Observation.ObserverRunningState.Active, 1));
        await EmittedCount(1);
        _queriesAfterInitialState = TimestampQueries;
        EmitDefinitions("first", "second", "third");
        await EmittedCount(2);
        EmitDefinitions("first");
        await EmittedCount(3);
    }

    [Fact] void should_not_query_timestamps() => TimestampQueries.ShouldEqual(_queriesAfterInitialState);
    [Fact] void should_emit_each_definition_change() => _emitted.Count.ShouldEqual(3);
}
