// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reactivated;

public class and_a_previous_clear_is_already_durable : given.an_alert_tracker
{
    async Task Establish()
    {
        await _tracker.Reconcile(_snapshot);
        _snapshot = _snapshot with { FailedPartitions = [], Revision = 2 };
        await _tracker.Reconcile(_snapshot);
        _appends.Clear();
        await CrashTracker();
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_not_clear_twice() => _appends.ShouldBeEmpty();
    [Fact] void should_acknowledge_application_from_the_history() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
