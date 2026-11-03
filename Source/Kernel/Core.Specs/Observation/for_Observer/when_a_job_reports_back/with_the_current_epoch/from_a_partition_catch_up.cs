// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_job_reports_back.with_the_current_epoch;

public class from_a_partition_catch_up : given.an_observer_holding_a_newer_claim
{
    Task Establish() => HoldPartition(ClaimHolderKind.PartitionCatchUp);

    Task Because() => _observer.PartitionCaughtUp(_partition, 44UL, 44UL, CurrentEpoch);

    [Fact] void should_complete_the_partition() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain(_partition);
    [Fact] void should_release_the_catch_up_claim() => (ClaimOnPartition?.Holder == ClaimHolderKind.PartitionCatchUp).ShouldBeFalse();
}
