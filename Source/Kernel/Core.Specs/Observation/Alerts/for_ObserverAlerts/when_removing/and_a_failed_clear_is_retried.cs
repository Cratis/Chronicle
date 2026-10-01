// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_removing;

public class and_a_failed_clear_is_retried : given.an_alert_tracker
{
    async Task Establish()
    {
        GivenHistory(RaisedForSnapshot());
        AppendReturns(AppendResult.Failed(CorrelationId.NotSet, (AppendError[])[new("Unavailable")]));
        await ReconcileRemoval();
        GivenHistory(RaisedForSnapshot());
        AppendSucceedsFrom(1);
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_clear_the_incident_once() => _appends.OfType<AlertCleared>().Count().ShouldEqual(1);
    [Fact] void should_keep_the_removal_reason() => ((AlertCleared)_appends.Single()).Reason.ShouldEqual(AlertClearedReason.Removed);
    [Fact] void should_acknowledge_application() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
    [Fact] void should_stop_retrying_after_success() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
