// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class after_reactivation_with_open_incident : given.an_alert_tracker
{
    void Establish() => GivenHistory(RaisedForSnapshot());

    async Task Because() => await _tracker.Reconcile(_snapshot);

    [Fact] void should_not_raise_the_persisted_incident_again() => _appends.ShouldBeEmpty();
}
