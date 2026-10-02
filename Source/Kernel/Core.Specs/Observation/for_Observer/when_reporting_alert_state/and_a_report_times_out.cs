// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Observation.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_alert_state;

public class and_a_report_times_out : given.an_observer
{
    bool _applied;

    async Task Establish()
    {
        _observerAlerts.Reconcile(Arg.Any<ObserverAlertSnapshot>()).Returns(Task.FromException<ObserverAlertReceipt>(new TimeoutException("Report timed out")));
        _applied = await ReportAlerts();
        ApplyAlertReports();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_not_acknowledge_uncertainty() => _applied.ShouldBeFalse();
    [Fact] async Task should_retry_even_though_the_observer_is_healthy() => await _observerAlerts.Received(1).Reconcile(Arg.Any<ObserverAlertSnapshot>());
}
