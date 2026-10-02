// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_the_observer_is_quarantined_with_an_existing_failure : given.a_reactivated_quarantined_observer
{
    void Establish()
    {
        _failedPartitionsState.AddFailedPartition("partition", 42UL);
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["Failed again"], "Stack");

    [Fact] async Task should_not_report_the_unchanged_failure_episode() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
    [Fact] void should_still_record_the_attempt() => _failedPartitionsState.Partitions.Single().Attempts.Count().ShouldEqual(2);
}
