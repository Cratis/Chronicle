// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_an_append_commits_but_the_response_is_lost : given.an_alert_tracker
{
    ObserverAlertReceipt _uncertain;

    async Task Establish()
    {
        AppendUsing(() =>
        {
            RecordDurable(_serialized);
            throw new TimeoutException("Response lost after commit");
        });
        _uncertain = await _tracker.Reconcile(_snapshot);
        await CrashTracker();
        AppendSucceedsFrom(1);
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_require_a_retry_after_uncertainty() => _uncertain.Outcome.ShouldEqual(ObserverAlertReconciliation.RetryRequired);
    [Fact] void should_not_append_the_raise_again() => _appends.ShouldBeEmpty();
    [Fact] void should_acknowledge_durable_application() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
