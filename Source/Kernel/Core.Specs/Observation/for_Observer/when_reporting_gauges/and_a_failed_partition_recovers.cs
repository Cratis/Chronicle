// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_gauges;

public class and_a_failed_partition_recovers : given.an_observer_reporting_gauges
{
    int _failedBeforeRecovery;

    async Task Establish()
    {
        await _observer.PartitionFailed("partition", 42UL, ["Something went wrong"], "stack trace");
        _failedBeforeRecovery = Entry!.Snapshot.FailedPartitions;
        EventSequenceDoesNotHaveNextEvent(42UL);
    }

    Task Because() => _observer.FailedPartitionRecovered("partition", 42UL);

    [Fact] void should_have_reported_the_failure() => _failedBeforeRecovery.ShouldEqual(1);
    [Fact] void should_report_no_failed_partitions() => Entry!.Snapshot.FailedPartitions.ShouldEqual(0);
    [Fact] void should_report_no_quarantined_partitions() => Entry!.Snapshot.QuarantinedPartitions.ShouldEqual(0);
}
