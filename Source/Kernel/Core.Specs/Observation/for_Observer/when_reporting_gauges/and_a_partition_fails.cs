// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_gauges;

public class and_a_partition_fails : given.an_observer_reporting_gauges
{
    Task Because() => _observer.PartitionFailed("partition", 42UL, ["Something went wrong"], "stack trace");

    [Fact] void should_report_one_failed_partition() => Entry!.Snapshot.FailedPartitions.ShouldEqual(1);
    [Fact] void should_report_no_quarantined_partitions() => Entry!.Snapshot.QuarantinedPartitions.ShouldEqual(0);
    [Fact] void should_not_release_it() => _released.ShouldBeEmpty();
}
