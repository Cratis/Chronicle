// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class after_reactivation_with_escalated_incident : given.an_alert_tracker
{
    void Establish()
    {
        var raised = RaisedForSnapshot();
        GivenHistory(raised, new AlertEscalated(raised.IncidentId, AlertConditionKind.PartitionRetriesExhausted, AlertSeverity.Critical, raised.Target, raised.Evidence));
        _snapshot = _snapshot with { FailedPartitions = [_snapshot.FailedPartitions.Single() with { IsQuarantined = true }] };
    }

    async Task Because() => await _tracker.Reconcile(_snapshot);

    [Fact] void should_not_raise_or_escalate_again() => _appends.ShouldBeEmpty();
}
