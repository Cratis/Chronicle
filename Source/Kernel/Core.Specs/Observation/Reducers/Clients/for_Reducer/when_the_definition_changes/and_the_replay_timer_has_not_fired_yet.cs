// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_the_definition_changes;

public class and_the_replay_timer_has_not_fired_yet : given.a_reducer_grain_with_replay_on_definition_change
{
    async Task Because() => await _grain.SetDefinitionAndSubscribe(_definition);

    [Fact] void should_register_the_replay_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(1);
}
