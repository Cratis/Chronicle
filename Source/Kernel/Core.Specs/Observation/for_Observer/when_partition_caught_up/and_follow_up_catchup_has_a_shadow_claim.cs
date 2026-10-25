// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_caught_up;

public class and_follow_up_catchup_has_a_shadow_claim : given.an_observer_with_one_partition_being_caught_up
{
    EventSequenceNumber _read;

    void Establish()
    {
        _read = 50UL;
        EventSequenceHasNextEvent(_read);
    }

    async Task Because() => await _observer.PartitionCaughtUp(_partition, EventSequenceNumber.Unavailable, _read);

    [Fact] void should_grant_the_partition_claim() => _stateStorage.State.PartitionClaims.Single().Partition.ShouldEqual(_partition);
    [Fact] void should_use_the_partition_catch_up_kind() => _stateStorage.State.PartitionClaims.Single().Holder.ShouldEqual(ClaimHolderKind.PartitionCatchUp);
    [Fact] void should_keep_the_requested_resume_position() => _stateStorage.State.PartitionClaims.Single().ResumePosition.ShouldEqual(_read + 1);
    [Fact] void should_not_invent_a_partition_job_id() => _stateStorage.State.PartitionClaims.Single().Job.ShouldBeNull();
    [Fact] void should_allocate_the_first_epoch() => _stateStorage.State.PartitionClaims.Single().Epoch.ShouldEqual(new ClaimEpoch(1));
}
