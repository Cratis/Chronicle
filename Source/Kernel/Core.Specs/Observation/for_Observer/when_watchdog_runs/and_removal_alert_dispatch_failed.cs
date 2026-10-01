// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_removal_alert_dispatch_failed : for_Observer.given.an_observer
{
    Exception _error;

    async Task Establish()
    {
        FailAlertReports(new InvalidOperationException("Dispatch failed"));
        _error = await Catch.Exception(_observer.Remove);
        ApplyAlertReports();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_fail_the_original_management_call() => _error.ShouldBeOfExactType<ObserverAlertsNotReconciled>();
    [Fact] async Task should_retry_the_removal_level_once() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.Disposition == AlertDisposition.Removing));
    [Fact] void should_retain_the_cleanup_fence() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Removing);
}
