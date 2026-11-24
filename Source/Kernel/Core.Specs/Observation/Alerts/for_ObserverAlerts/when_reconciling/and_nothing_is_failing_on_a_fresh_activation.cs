// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

/// <summary>
/// A fresh activation cannot know whether an incident was left open by an earlier one, so even a healthy report drains
/// the preceding appends and reads the history before it concludes there is nothing to close.
/// </summary>
public class and_nothing_is_failing_on_a_fresh_activation : given.an_alert_tracker
{
    void Establish() => GivenHistory(RaisedForSnapshot());

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot with { FailedPartitions = [] });

    [Fact] async Task should_drain_appends() => await _sequence.Received(1).DrainAppends();
    [Fact] void should_close_the_incident_left_open() => _appends.Count.ShouldEqual(1);
}
