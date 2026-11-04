// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_job_reports_back.with_the_current_epoch;

public class from_a_partition_replay : given.an_observer_holding_a_newer_claim
{
    Task Establish() => HoldPartition(ClaimHolderKind.PartitionReplay);

    Task Because() => _observer.PartitionReplayed(_partition, 44UL, 44UL, [], CurrentEpoch);

    [Fact] void should_complete_the_partition() => _stateStorage.State.ReplayingPartitions.ShouldNotContain(_partition);
    [Fact] void should_release_the_replay_claim() => (ClaimOnPartition?.Holder == ClaimHolderKind.PartitionReplay).ShouldBeFalse();
}
