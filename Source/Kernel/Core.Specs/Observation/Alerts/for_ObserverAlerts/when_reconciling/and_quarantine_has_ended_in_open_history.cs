// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_quarantine_has_ended_in_open_history : given.an_alert_tracker
{
    IncidentId _incidentId;

    void Establish()
    {
        _incidentId = IncidentId.New();
        GivenHistory(new AlertRaised(_incidentId, AlertConditionKind.ObserverQuarantined, AlertSeverity.Critical, AlertTarget.For(_key, AlertPartition.None), AlertEvidence.Create(0, _clock.Now, _clock.Now, FailureKind.Unknown, string.Empty)));
        _snapshot = _snapshot with { FailedPartitions = [], Endings = new Dictionary<IncidentId, AlertClearedReason> { [_incidentId] = AlertClearedReason.Revived } };
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_clear_the_original_quarantine_incident() => ((AlertCleared)_appends.Single()).IncidentId.ShouldEqual(_incidentId);
    [Fact] void should_preserve_the_quarantine_clear_reason() => ((AlertCleared)_appends.Single()).Reason.ShouldEqual(AlertClearedReason.Revived);
    [Fact] void should_acknowledge_application() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
