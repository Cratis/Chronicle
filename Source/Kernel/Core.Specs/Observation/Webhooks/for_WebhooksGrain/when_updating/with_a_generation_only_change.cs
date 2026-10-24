// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhooksGrain.when_updating;

public class with_a_generation_only_change : given.a_webhooks_manager_grain
{
    WebhookDefinition _definition;
    IObserver _observer;
    void Establish()
    {
        _definition = new("webhook", WebhookOwner.Client, "log", [new EventType("customer", 1)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()))
        {
            GenerationDelivery = EventGenerationDelivery.Pinned
        };
        _stateStorage.State.Webhooks = [_definition];
        _namespacesGrain.GetAll().Returns(["namespace"]);
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
    }

    async Task Because() => await _grain.Update(_definition with { EventTypes = [new EventType("customer", 3)] });
    [Fact] async Task should_rebind_the_changed_pin() => await _observer.Received(1).SubscribeWithGenerationDelivery<IWebhookObserverSubscriber>(ObserverType.External, Arg.Is<IEnumerable<EventType>>(_ => _.Single() == new EventType("customer", 3)), Arg.Any<SiloAddress>(), EventGenerationDelivery.Pinned, EventGenerationDelivery.Pinned, reactivateRetired: true, automatic: false);
    [Fact] async Task should_not_unsubscribe() => await _observer.DidNotReceive().Unsubscribe();
    [Fact] void should_persist_the_changed_pin() => _stateStorage.State.Webhooks.Single().EventTypes.Single().Generation.Value.ShouldEqual(3u);
}
