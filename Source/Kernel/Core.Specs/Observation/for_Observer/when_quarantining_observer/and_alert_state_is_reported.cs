// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_quarantining_observer;

public class and_alert_state_is_reported : given.an_observer_with_subscription
{
    async Task Because()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        await ReportAlerts();
    }

    [Fact] async Task should_report_the_observer_quarantine() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.Observer == _observerKey && snapshot.IsQuarantined));
}
