// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.States.for_Routing.when_entering;

public class and_a_healthy_partition_also_has_unhandled_events : given.a_routing_state_with_a_failed_partition
{
    void Establish() => _partitionsWithEventsBehind = [_failedPartition, _healthyPartition];

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_start_catch_up() => _observer.Received(1).CatchUp();
}
