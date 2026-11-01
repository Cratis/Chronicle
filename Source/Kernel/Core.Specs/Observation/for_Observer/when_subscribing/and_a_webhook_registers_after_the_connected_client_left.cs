// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_webhook_registers_after_the_connected_client_left : given.an_observer_served_by_a_connected_client
{
    async Task Because()
    {
        await _observer.UnsubscribeIfMatchesClient(_client.ConnectionId);
        await _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, [event_type], SiloAddress.Zero);
    }

    [Fact] async Task should_subscribe_the_webhook() => (await _observer.GetSubscription()).SubscriberType.ShouldEqual(typeof(IWebhookObserverSubscriber));
}
