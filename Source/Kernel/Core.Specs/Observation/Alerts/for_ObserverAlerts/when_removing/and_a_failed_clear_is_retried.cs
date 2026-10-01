// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_removing;

public class and_a_failed_clear_is_retried : given.an_alert_tracker
{
    async Task Establish()
    {
        GivenHistory(RaisedForSnapshot());
        AppendReturns(AppendResult.Failed(CorrelationId.NotSet, (AppendError[])[new("Unavailable")]));
        await _tracker.Removed();
        GivenHistory(RaisedForSnapshot());
        AppendSucceedsFrom(1);
    }

    async Task Because() => await _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_clear_the_incident_once() => _appends.OfType<AlertCleared>().Count().ShouldEqual(1);
    [Fact] void should_keep_the_removal_reason() => ((AlertCleared)_appends.Single()).Reason.ShouldEqual(AlertClearedReason.Removed);
    [Fact] async Task should_leave_no_open_incidents() => (await _tracker.HasOpenIncidents()).ShouldBeFalse();
    [Fact] void should_stop_retrying_after_success() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
