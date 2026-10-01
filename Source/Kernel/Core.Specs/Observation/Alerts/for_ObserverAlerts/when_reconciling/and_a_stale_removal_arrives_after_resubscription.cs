// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_a_stale_removal_arrives_after_resubscription : given.an_alert_tracker
{
    ObserverAlertSnapshot _stale;

    void Establish()
    {
        GivenHistory(RaisedForSnapshot());
        _stale = _snapshot with { Disposition = AlertDisposition.Removing };
        _snapshot = _snapshot with { LifecycleId = Guid.NewGuid() };
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_stale);

    [Fact] void should_reject_the_old_lifecycle() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Superseded);
    [Fact] void should_not_clear_the_current_incident() => _appends.ShouldBeEmpty();
}
