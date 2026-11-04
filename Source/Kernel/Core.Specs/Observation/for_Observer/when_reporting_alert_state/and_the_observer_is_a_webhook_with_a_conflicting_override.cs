// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_alert_state;

public class and_the_observer_is_a_webhook_with_a_conflicting_override : given.a_webhook_observer
{
    protected override Observers CreateObserversConfig() => new() { WriteCoalescingInterval = 0, MaxRetryAttempts = 1 };
    protected override Observers CreateWebhookRetryConfig() => new() { WriteCoalescingInterval = 0, MaxRetryAttempts = 0 };

    async Task Because() => await _observer.ReportAlertState();

    [Fact] async Task should_report_the_webhook_retry_limit() => await _observerAlerts.Received().Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.MaxRetryAttempts == 0));
}
