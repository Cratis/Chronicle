// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_recording_bulk_failures;

public class and_multiple_partitions_fail : given.an_observer
{
    async Task Because() => await _observer.PartitionsFailed([new("first", 42UL) { Reason = "Duplicate key" }, new("second", 43UL)]);

    [Fact] void should_record_every_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(2);
    [Fact] async Task should_report_only_one_complete_snapshot() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Count == 2 && snapshot.FailedPartitions.All(partition => partition.FailureKind == FailureKind.Handling)));
    [Fact] void should_preserve_the_sink_reason() => _failedPartitionsState.Partitions.Single(partition => partition.Partition.ToString() == "first").LastAttempt.Messages.Single().ShouldContain("Duplicate key");
}
