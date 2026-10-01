// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reactivated;

public class and_the_previous_removal_belongs_to_an_old_lifecycle : given.an_alert_tracker
{
    ObserverAlertSnapshot _old;

    async Task Establish()
    {
        await ReconcileRemoval();
        _old = _snapshot;
        _snapshot = _snapshot with { LifecycleId = Guid.NewGuid(), Disposition = AlertDisposition.Active };
        GivenHistory(RaisedForSnapshot());
        await CrashTracker();
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_old);

    [Fact] void should_validate_against_the_durable_source() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Superseded);
    [Fact] void should_not_clear_the_new_lifecycle() => _appends.ShouldBeEmpty();
}
