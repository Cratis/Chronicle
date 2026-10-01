// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.for_Observer.given;

namespace Cratis.Chronicle.Observation.for_ProjectionBulkFailures.when_recording;

public class and_multiple_partitions_failed : an_observer
{
    IGrainFactory _grainFactory;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IObserver>(_observerKey).Returns(_observer);
    }

    async Task Because()
    {
        await ProjectionBulkFailures.Record(_grainFactory, new(_observerKey, ObserverType.Projection), [new("first", 42UL), new("second", 43UL)]);
        await ReportAlerts();
    }

    [Fact] void should_record_every_failed_partition() => _failedPartitionsState.Partitions.Count().ShouldEqual(2);
    [Fact] async Task should_report_one_complete_alert_snapshot() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Count == 2 && snapshot.FailedPartitions.All(partition => partition.FailureKind == FailureKind.Handling)));
}
