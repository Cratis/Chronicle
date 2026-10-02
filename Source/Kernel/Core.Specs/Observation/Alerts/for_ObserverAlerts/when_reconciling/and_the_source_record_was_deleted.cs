// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_the_source_record_was_deleted : given.an_alert_tracker
{
    void Establish() => _source = ObserverState.Empty;

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_supersede_the_report() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Superseded);
    [Fact] void should_not_authorize_a_late_raise() => _appends.ShouldBeEmpty();
}
