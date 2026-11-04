// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering_with_a_partition_registered_by_the_observer_catch_up;

public class and_the_observer_catch_up_runs_under_another_epoch : given.a_partition_registered_by_the_observer_catch_up
{
    void Establish() => ObserverCatchUpIsOutstandingWith(WideClaimEpoch - 1);

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_start_a_partition_catch_up_from_the_saved_position() => _started.Single().FromSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_remove_the_marker_once_the_job_is_durable() => _resultingStoredState.InFlightPartitions.ShouldBeEmpty();
}
