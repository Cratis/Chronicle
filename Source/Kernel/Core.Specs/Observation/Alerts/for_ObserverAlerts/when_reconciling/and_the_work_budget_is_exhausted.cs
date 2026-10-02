// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_the_work_budget_is_exhausted : given.an_alert_tracker
{
    ObserverAlertReceipt _bounded;

    async Task Establish()
    {
        var partition = _snapshot.FailedPartitions.Single();
        _snapshot = _snapshot with
        {
            FailedPartitions = Enumerable.Range(0, ObserverAlerts.MaximumTransitionsPerReport + 1)
                .Select(index => partition with { Id = FailedPartitionId.New(), Partition = index.ToString(System.Globalization.CultureInfo.InvariantCulture) }).ToArray()
        };
        _bounded = await _tracker.Reconcile(_snapshot);
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_leave_application_outstanding_at_the_boundary() => _bounded.Outcome.ShouldEqual(ObserverAlertReconciliation.RetryRequired);
    [Fact] void should_complete_without_duplicate_transitions() => _appends.Count.ShouldEqual(ObserverAlerts.MaximumTransitionsPerReport + 1);
    [Fact] void should_acknowledge_after_the_next_report() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
