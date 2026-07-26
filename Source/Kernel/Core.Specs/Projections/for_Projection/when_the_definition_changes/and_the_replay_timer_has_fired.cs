// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_the_definition_changes;

public class and_the_replay_timer_has_fired : given.a_projection_grain_with_replay_on_definition_change
{
    async Task Establish() => await _grain.SetDefinition(_definition);

    async Task Because() => await _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_dispose_the_replay_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
