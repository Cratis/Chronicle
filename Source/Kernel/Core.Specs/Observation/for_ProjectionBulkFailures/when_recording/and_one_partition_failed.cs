// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.for_Observer.given;

namespace Cratis.Chronicle.Observation.for_ProjectionBulkFailures.when_recording;

public class and_one_partition_failed : an_observer
{
    IGrainFactory _grainFactory;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IObserver>(_observerKey).Returns(_observer);
    }

    async Task Because()
    {
        await ProjectionBulkFailures.Record(_grainFactory, new(_observerKey, ObserverType.Projection), [new("partition", 42UL) { Reason = "Duplicate key" }]);
        await ReportAlerts();
    }

    [Fact] void should_record_the_failed_partition() => _failedPartitionsState.Partitions.Single().Partition.ToString().ShouldEqual("partition");
    [Fact] void should_keep_the_failure_sequence_number() => _failedPartitionsState.Partitions.Single().LastAttempt.SequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_keep_the_sink_reason() => _failedPartitionsState.Partitions.Single().LastAttempt.Messages.Single().ShouldContain("Duplicate key");
    [Fact] async Task should_report_one_alert_snapshot() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Count == 1 && snapshot.FailedPartitions.Single().FailureKind == FailureKind.Handling));
}
