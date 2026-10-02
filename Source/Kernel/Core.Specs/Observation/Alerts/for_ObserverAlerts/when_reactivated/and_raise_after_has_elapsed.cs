// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reactivated;

public class and_raise_after_has_elapsed : given.an_alert_tracker
{
    async Task Establish()
    {
        _snapshot = _snapshot with { FailedPartitions = [_snapshot.FailedPartitions.Single() with { FirstAttempt = _clock.Now }] };
        await _tracker.Reconcile(_snapshot);
        await CrashTracker();
        _clock.Now += TimeSpan.FromMinutes(6);
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_raise_from_the_current_source_report() => _appends.Single().ShouldBeOfExactType<AlertRaised>();
    [Fact] void should_acknowledge_application() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
