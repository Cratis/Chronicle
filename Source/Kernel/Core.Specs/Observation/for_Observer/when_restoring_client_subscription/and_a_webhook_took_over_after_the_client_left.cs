// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_a_webhook_took_over_after_the_client_left : given.an_observer_served_by_a_connected_client
{
    async Task Establish()
    {
        await _observer.UnsubscribeIfMatchesClient(_client.ConnectionId);
        await _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, [event_type], SiloAddress.Zero);
    }

    Task Because() => _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);

    [Fact] async Task should_keep_the_webhook_subscriber() => (await _observer.GetSubscription()).SubscriberType.ShouldEqual(typeof(IWebhookObserverSubscriber));
    [Fact] async Task should_not_know_the_waiting_client() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeFalse();
}
