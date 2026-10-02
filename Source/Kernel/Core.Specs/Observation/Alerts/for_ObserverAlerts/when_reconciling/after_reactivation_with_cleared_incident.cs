// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class after_reactivation_with_cleared_incident : given.an_alert_tracker
{
    void Establish()
    {
        var raised = RaisedForSnapshot();
        GivenHistory(raised, new AlertCleared(raised.IncidentId, raised.Condition, AlertClearedReason.Recovered, raised.Target));
        _snapshot = _snapshot with { FailedPartitions = [] };
    }

    async Task Because() => await _tracker.Reconcile(_snapshot);

    [Fact] void should_not_clear_again() => _appends.ShouldBeEmpty();
}
