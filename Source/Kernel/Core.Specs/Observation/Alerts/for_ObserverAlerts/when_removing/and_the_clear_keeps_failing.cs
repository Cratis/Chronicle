// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_removing;

public class and_the_clear_keeps_failing : given.an_alert_tracker
{
    int _timersAfterFailure;

    async Task Establish()
    {
        GivenHistory(RaisedForSnapshot());
        AppendReturns(AppendResult.Failed(CorrelationId.NotSet, (AppendError[])[new("Unavailable")]));
        await _tracker.Removed();
    }

    async Task Because()
    {
        GivenHistory(RaisedForSnapshot());
        await _silo.TimerRegistry.FireAllAsync();
        _timersAfterFailure = _silo.TimerRegistry.NumberOfActiveTimers;
        GivenHistory(RaisedForSnapshot());
        AppendSucceedsFrom(1);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_keep_one_retry_timer_after_another_failure() => _timersAfterFailure.ShouldEqual(1);
    [Fact] void should_clear_the_incident_once_after_recovery() => _appends.OfType<AlertCleared>().Count().ShouldEqual(1);
    [Fact] void should_preserve_the_removed_reason_across_attempts() => ((AlertCleared)_appends.Single()).Reason.ShouldEqual(AlertClearedReason.Removed);
    [Fact] void should_stop_retrying_after_recovery() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
