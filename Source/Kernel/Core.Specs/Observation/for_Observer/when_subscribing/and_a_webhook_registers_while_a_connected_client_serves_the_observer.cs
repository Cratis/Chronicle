// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_webhook_registers_while_a_connected_client_serves_the_observer : given.an_observer_served_by_a_connected_client
{
    Exception _exception;
    ObserverSubscription _subscriptionBefore;
    ObserverDefinition _definitionBefore;

    async Task Establish()
    {
        _subscriptionBefore = await _observer.GetSubscription();
        _definitionBefore = _definitionStorage.State;
    }

    async Task Because() => _exception = await Catch.Exception(() => _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, [event_type], SiloAddress.Zero));

    [Fact] void should_refuse_the_takeover() => _exception.ShouldBeOfExactType<ObserverServedByAnotherTransport>();
    [Fact] async Task should_keep_the_subscription_unchanged() => (await _observer.GetSubscription()).ShouldEqual(_subscriptionBefore);
    [Fact] void should_keep_the_definition_unchanged() => _definitionStorage.State.ShouldEqual(_definitionBefore);
    [Fact] async Task should_keep_the_client_target() => (await _observer.GetSubscription()).Targets.Single().ConnectedClient.ShouldEqual(_client);
    [Fact] async Task should_keep_the_subscriber_type() => (await _observer.GetSubscription()).SubscriberType.ShouldEqual(typeof(NullObserverSubscriber));
}
