// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_quarantine_revival_is_reported : given.an_observer_with_subscription
{
    async Task Establish()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        await ReportAlerts();
    }

    [Fact] async Task should_report_the_ended_quarantine_as_revived() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => !snapshot.IsQuarantined && snapshot.Endings.Values.Contains(AlertClearedReason.Revived)));
}
