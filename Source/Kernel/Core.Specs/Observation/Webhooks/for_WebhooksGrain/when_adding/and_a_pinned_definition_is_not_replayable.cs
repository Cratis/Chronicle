// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhooksGrain.when_adding;

public class and_a_pinned_definition_is_not_replayable : given.a_webhooks_manager_grain
{
    IObserver _observer;
    WebhookDefinition _definition;

    void Establish()
    {
        _definition = new("webhook", WebhookOwner.Client, "log", [new EventType("customer", 3)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()), IsReplayable: false)
        {
            GenerationDelivery = EventGenerationDelivery.Pinned
        };
        _namespacesGrain.GetAll().Returns(["namespace"]);
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
    }

    Task Because() => _grain.Add(_definition);

    [Fact] async Task should_subscribe_without_replayability() => await _observer.Received(1).SubscribeWithGenerationDelivery<IWebhookObserverSubscriber>(ObserverType.External, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), EventGenerationDelivery.Pinned, EventGenerationDelivery.Pinned, isReplayable: false, reactivateRetired: true, automatic: false);
}
