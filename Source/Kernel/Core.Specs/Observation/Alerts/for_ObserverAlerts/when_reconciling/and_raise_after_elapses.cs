// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_raise_after_elapses : given.an_alert_tracker
{
    async Task Establish()
    {
        _snapshot = _snapshot with { FailedPartitions = [_snapshot.FailedPartitions.Single() with { FirstAttempt = _clock.Now }] };
        await _tracker.Reconcile(_snapshot);
        _appends.ShouldBeEmpty();
        _clock.Now += TimeSpan.FromMinutes(5);
    }

    async Task Because() => await _silo.TimerRegistry.FireAllAsync();

    [Fact] void should_raise_once_from_the_timer() => _appends.OfType<AlertRaised>().Count().ShouldEqual(1);
    [Fact] void should_dispose_the_one_shot_timer() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
}
