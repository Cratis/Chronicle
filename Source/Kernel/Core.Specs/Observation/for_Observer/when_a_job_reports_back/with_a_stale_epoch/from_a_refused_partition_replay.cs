// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_a_job_reports_back.with_a_stale_epoch;

public class from_a_refused_partition_replay : given.an_observer_holding_a_newer_claim
{
    Task Establish() => HoldPartition(ClaimHolderKind.PartitionReplay);

    Task Because() => _observer.ReleaseRefusedPartitionReplay(_partition, 45UL, StaleEpoch);

    [Fact] void should_keep_the_partition_replaying() => _stateStorage.State.ReplayingPartitions.ShouldContain(_partition);
    [Fact] void should_keep_the_newer_claim() => ClaimOnPartition!.Epoch.ShouldEqual(new ClaimEpoch(CurrentEpoch));
    [Fact] void should_keep_admitting_the_newer_job() => AdmitsNewerPartitionReplay().ShouldBeTrue();
}
