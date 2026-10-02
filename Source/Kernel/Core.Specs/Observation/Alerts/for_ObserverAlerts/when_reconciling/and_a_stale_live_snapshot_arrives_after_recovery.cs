// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_a_stale_live_snapshot_arrives_after_recovery : given.an_alert_tracker
{
    ObserverAlertSnapshot _stale;

    void Establish()
    {
        _stale = _snapshot;
        _snapshot = _snapshot with { FailedPartitions = [], Revision = 2 };
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_stale);

    [Fact] void should_reject_the_old_revision() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Superseded);
    [Fact] void should_not_raise_the_ended_failure() => _appends.ShouldBeEmpty();
}
