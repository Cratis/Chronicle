// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States.for_CatchingUpInFlight.when_entering_with_a_partition_registered_by_the_observer_catch_up;

/// <summary>
/// A partition catch-up claim means the hold was granted for a partition catch-up, which only its own durable job owns.
/// None is outstanding here, so the hold was left by a crash before that job started.
/// </summary>
public class and_a_partition_catch_up_claims_the_partition : given.a_partition_registered_by_the_observer_catch_up
{
    void Establish() => _storedState = _storedState with
    {
        PartitionClaims = [new(_partition, ClaimHolderKind.PartitionCatchUp, null, 3UL, 42UL, [])]
    };

    async Task Because() => _resultingStoredState = await _state.OnEnter(_storedState);

    [Fact] void should_start_a_partition_catch_up_from_the_saved_position() => _started.Single().FromSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_remove_the_marker_once_the_job_is_durable() => _resultingStoredState.InFlightPartitions.ShouldBeEmpty();
}
