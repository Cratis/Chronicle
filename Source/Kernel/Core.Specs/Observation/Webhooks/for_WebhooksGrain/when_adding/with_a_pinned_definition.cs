// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhooksGrain.when_adding;

public class with_a_pinned_definition : given.a_webhooks_manager_grain
{
    WebhookDefinition _definition;
    IObserver _observer;
    void Establish()
    {
        _definition = new("webhook", WebhookOwner.Client, "log", [new EventType("customer", 3)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()))
        {
            GenerationDelivery = EventGenerationDelivery.Pinned
        };
        _namespacesGrain.GetAll().Returns(["namespace"]);
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
    }

    async Task Because() => await _grain.Add(_definition);
    [Fact] async Task should_subscribe_with_the_pinned_policy() => await _observer.Received(1).SubscribeWithGenerationDelivery<IWebhookObserverSubscriber>(ObserverType.External, Arg.Is<IEnumerable<EventType>>(_ => _.Single() == new EventType("customer", 3)), Arg.Any<SiloAddress>(), EventGenerationDelivery.Pinned, EventGenerationDelivery.Pinned);
    [Fact] void should_persist_the_policy() => _stateStorage.State.Webhooks.Single().GenerationDelivery.ShouldEqual(EventGenerationDelivery.Pinned);
}
