// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_history_is_long : given.an_alert_tracker
{
    async Task Establish()
    {
        for (var index = 0; index < 10000; index++)
        {
            var raised = RaisedForSnapshot() with { IncidentId = IncidentId.New() };
            RecordDurable(raised);
            RecordDurable(new AlertCleared(raised.IncidentId, raised.Condition, AlertClearedReason.Recovered, raised.Target));
        }

        _snapshot = _snapshot with { FailedPartitions = [] };
        await _tracker.Reconcile(_snapshot);
        _serializer.ClearReceivedCalls();
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_not_refold_twenty_thousand_old_transitions() => _serializer.DidNotReceive().Deserialize(Arg.Any<AppendedEvent>());
    [Fact] void should_acknowledge_the_current_level() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
