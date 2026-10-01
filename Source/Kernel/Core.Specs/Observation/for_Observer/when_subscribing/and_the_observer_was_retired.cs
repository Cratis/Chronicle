// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_the_observer_was_retired : given.an_observer_with_subscription
{
    async Task Establish() => await _observer.Retire();

    async Task Because()
    {
        await _observer.Subscribe<ObserverSubscriber>(ObserverType.Projection, [], SiloAddress.Zero);
        _observerAlerts.ClearReceivedCalls();
        await _observer.PartitionFailed("partition", 12UL, ["Failed"], "Stack");
    }

    [Fact] async Task should_report_new_failures_again() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(snapshot => snapshot.FailedPartitions.Count == 1));
}
