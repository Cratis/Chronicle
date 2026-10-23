// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhooksGrain.when_adding;

public class with_a_compatibility_definition : given.a_webhooks_manager_grain
{
    IObserver _observer;
    WebhookDefinition _definition;

    void Establish()
    {
        _definition = new WebhookDefinition("webhook", WebhookOwner.Client, "log", [new EventType("customer", 3)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()))
        {
            GenerationDelivery = EventGenerationDelivery.Compatibility
        };
        _namespacesGrain.GetAll().Returns(["namespace"]);
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
    }

    Task Because() => _grain.Add(_definition);

    [Fact] async Task should_subscribe_as_explicit_registration() => await _observer.Received(1).Subscribe<IWebhookObserverSubscriber>(ObserverType.External, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), reactivateRetired: true, automatic: false);
}
