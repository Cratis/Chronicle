// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts;

public class when_removed : given.an_alert_tracker
{
    void Establish() => GivenHistory(RaisedForSnapshot());

    async Task Because() => await _tracker.Removed();

    [Fact] void should_clear_the_open_incident_as_removed() => _appends.OfType<AlertCleared>().Single().Reason.ShouldEqual(AlertClearedReason.Removed);
    [Fact] void should_not_raise_any_incidents() => _appends.OfType<AlertRaised>().ShouldBeEmpty();
}
