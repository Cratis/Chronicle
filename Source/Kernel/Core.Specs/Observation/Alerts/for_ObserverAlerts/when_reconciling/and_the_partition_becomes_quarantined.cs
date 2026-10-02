// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_the_partition_becomes_quarantined : given.an_alert_tracker
{
    async Task Establish() => await _tracker.Reconcile(_snapshot);

    async Task Because() => await _tracker.Reconcile(_snapshot with { FailedPartitions = [_snapshot.FailedPartitions.Single() with { IsQuarantined = true }] });

    [Fact] void should_have_raised_only_once() => _appends.OfType<AlertRaised>().Count().ShouldEqual(1);
    [Fact] void should_escalate_the_same_incident() => _appends.OfType<AlertEscalated>().Single().IncidentId.ShouldEqual(_appends.OfType<AlertRaised>().Single().IncidentId);
    [Fact] void should_escalate_to_retries_exhausted() => _appends.OfType<AlertEscalated>().Single().Condition.ShouldEqual(AlertConditionKind.PartitionRetriesExhausted);
    [Fact] void should_escalate_to_critical() => _appends.OfType<AlertEscalated>().Single().Severity.ShouldEqual(AlertSeverity.Critical);
}
