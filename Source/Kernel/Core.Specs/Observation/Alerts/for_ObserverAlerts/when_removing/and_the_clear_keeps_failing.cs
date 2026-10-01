// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_removing;

public class and_the_clear_keeps_failing : given.an_alert_tracker
{
    ObserverAlertReceipt _failed;

    async Task Establish()
    {
        GivenHistory(RaisedForSnapshot());
        AppendReturns(AppendResult.Failed(CorrelationId.NotSet, (AppendError[])[new("Unavailable")]));
        await ReconcileRemoval();
        _failed = await _tracker.Reconcile(_snapshot);
        AppendSucceedsFrom(1);
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_keep_application_outstanding_after_failure() => _failed.Outcome.ShouldEqual(ObserverAlertReconciliation.RetryRequired);
    [Fact] void should_clear_the_incident_once_after_recovery() => _appends.OfType<AlertCleared>().Count().ShouldEqual(1);
    [Fact] void should_preserve_the_removed_reason_across_attempts() => ((AlertCleared)_appends.Single()).Reason.ShouldEqual(AlertClearedReason.Removed);
    [Fact] void should_acknowledge_application_after_recovery() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
