// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_quarantine_identity_needs_legacy_adoption : given.an_alert_tracker
{
    readonly IncidentId _existing = IncidentId.New();

    void Establish()
    {
        GivenHistory(new AlertRaised(_existing, AlertConditionKind.ObserverQuarantined, AlertSeverity.Critical, AlertTarget.For(_key, AlertPartition.None), RaisedForSnapshot().Evidence));
        _snapshot = _snapshot with { IsQuarantined = true, QuarantineEpisodeId = null, FailedPartitions = [] };
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_propose_the_continuing_legacy_episode() => _receipt.QuarantineEpisodeId.ShouldEqual(_existing.Value);
    [Fact] void should_require_source_persistence_before_application() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.RetryRequired);
    [Fact] void should_not_change_history_during_adoption() => _appends.ShouldBeEmpty();
}
