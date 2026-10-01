// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_failing_partition;

public class and_the_observer_is_quarantined_with_a_new_failure : given.a_reactivated_quarantined_observer
{
    void Establish() => _observerAlerts.ClearReceivedCalls();

    async Task Because() => await _observer.PartitionFailed("partition", 42UL, ["Failed"], "Stack");

    [Fact] async Task should_report_the_new_failure_episode() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.IsQuarantined && snapshot.FailedPartitions.Single().AttemptCount == 1));
}
