// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_quarantine_has_ended_in_open_history : given.an_alert_tracker
{
    IncidentId _incidentId;
    bool _wasOpen;

    async Task Establish()
    {
        _incidentId = IncidentId.New();
        GivenHistory(new AlertRaised(_incidentId, AlertConditionKind.ObserverQuarantined, AlertSeverity.Critical, AlertTarget.For(_key, AlertPartition.None), AlertEvidence.Create(0, _clock.Now, _clock.Now, FailureKind.Unknown, string.Empty)));
        _wasOpen = await _tracker.HasOpenIncidents();
        _snapshot = _snapshot with { FailedPartitions = [], QuarantineEndedAs = AlertClearedReason.Revived };
    }

    async Task Because() => await _tracker.Reconcile(_snapshot);

    [Fact] void should_load_the_open_incident_before_reconciliation() => _wasOpen.ShouldBeTrue();
    [Fact] void should_clear_the_original_quarantine_incident() => ((AlertCleared)_appends.Single()).IncidentId.ShouldEqual(_incidentId);
    [Fact] void should_preserve_the_quarantine_clear_reason() => ((AlertCleared)_appends.Single()).Reason.ShouldEqual(AlertClearedReason.Revived);
    [Fact] async Task should_have_no_incidents_left_open() => (await _tracker.HasOpenIncidents()).ShouldBeFalse();
}
