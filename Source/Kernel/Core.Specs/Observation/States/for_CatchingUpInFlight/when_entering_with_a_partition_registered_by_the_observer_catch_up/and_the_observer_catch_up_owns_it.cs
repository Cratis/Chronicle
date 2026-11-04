// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering_with_a_partition_registered_by_the_observer_catch_up;

public class and_the_observer_catch_up_owns_it : given.a_partition_registered_by_the_observer_catch_up
{
    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_not_start_a_partition_catch_up() => ShouldStartNoPartitionCatchUp();
    [Fact] void should_keep_the_marker_for_the_observer_catch_up() => _resultingStoredState.InFlightPartitions.ShouldContain(_partition);
    [Fact] void should_keep_the_partition_held() => _resultingStoredState.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_go_on_routing() => _stateMachine.Received(1).TransitionTo<Routing>();
}
