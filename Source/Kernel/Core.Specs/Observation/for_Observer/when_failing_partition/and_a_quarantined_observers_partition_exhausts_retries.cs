// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_a_quarantined_observers_partition_exhausts_retries : given.a_reactivated_quarantined_observer
{
    protected override Observers CreateObserversConfig() => new() { MaxRetryAttempts = 1 };

    void Establish()
    {
        _failedPartitionsState.AddFailedPartition("partition", 42UL);
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.PartitionFailed("partition", 42UL, ["Failed again"], "Stack");
        await _observer.PartitionFailed("partition", 42UL, ["Failed yet again"], "Stack");
    }

    [Fact] async Task should_report_only_the_crossing_of_the_retry_limit() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.IsQuarantined && !snapshot.FailedPartitions.Single().IsQuarantined && snapshot.FailedPartitions.Single().AttemptCount == 2));
    [Fact] void should_record_later_attempts_without_reporting_them() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(3);
}
