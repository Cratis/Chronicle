// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_removing;

public class and_a_rejected_clear_is_already_durable : given.an_alert_tracker
{
    async Task Establish()
    {
        var raised = RaisedForSnapshot();
        GivenHistory(raised);
        AppendReturns(AppendResult.Failed(CorrelationId.NotSet, (AppendError[])[new("Ambiguous result")]));
        await ReconcileRemoval();
        GivenHistory(raised, new AlertCleared(raised.IncidentId, raised.Condition, AlertClearedReason.Removed, raised.Target));
        AppendSucceedsFrom(2);
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_not_append_the_clear_again() => _appends.ShouldBeEmpty();
    [Fact] void should_acknowledge_application() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
