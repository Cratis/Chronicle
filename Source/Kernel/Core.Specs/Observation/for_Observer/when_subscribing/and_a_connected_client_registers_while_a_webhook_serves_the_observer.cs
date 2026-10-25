// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_connected_client_registers_while_a_webhook_serves_the_observer : given.a_webhook_observer_with_a_waiting_client
{
    Exception _exception;
    ObserverSubscription _subscriptionBefore;
    ObserverDefinition _definitionBefore;

    async Task Establish()
    {
        _subscriptionBefore = await _observer.GetSubscription();
        _definitionBefore = _definitionStorage.State;
    }

    async Task Because() => _exception = await Catch.Exception(() => _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, ["Event1", "Event2"], SiloAddress.Zero, _client));

    [Fact] void should_refuse_the_takeover() => _exception.ShouldBeOfExactType<ObserverServedByAnotherTransport>();
    [Fact] async Task should_keep_the_subscription_unchanged() => (await _observer.GetSubscription()).ShouldEqual(_subscriptionBefore);
    [Fact] void should_keep_the_definition_unchanged() => _definitionStorage.State.ShouldEqual(_definitionBefore);
    [Fact] async Task should_keep_the_webhook_subscriber() => (await _observer.GetSubscription()).SubscriberType.ShouldEqual(typeof(IWebhookObserverSubscriber));
    [Fact] async Task should_not_know_the_waiting_client() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeFalse();
}
