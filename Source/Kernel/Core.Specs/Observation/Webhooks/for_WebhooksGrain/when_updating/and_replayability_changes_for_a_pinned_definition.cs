// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhooksGrain.when_updating;

public class and_replayability_changes_for_a_pinned_definition : given.a_webhooks_manager_grain
{
    IObserver _observer;
    WebhookDefinition _definition;

    async Task Establish()
    {
        _definition = new("webhook", WebhookOwner.Client, "log", [new EventType("customer", 3)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()))
        {
            GenerationDelivery = EventGenerationDelivery.Pinned
        };
        _namespacesGrain.GetAll().Returns(["namespace"]);
        _observer = Substitute.For<IObserver>();
        _observer.IsSubscribed().Returns(true);
        _silo.AddProbe(_ => _observer);
        await _grain.Add(_definition);
        _observer.ClearReceivedCalls();
    }

    Task Because() => _grain.Update(_definition with { IsReplayable = false });

    [Fact] async Task should_resubscribe_without_replayability() => await _observer.Received(1).SubscribeWithGenerationDelivery<IWebhookObserverSubscriber>(ObserverType.External, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), EventGenerationDelivery.Pinned, EventGenerationDelivery.Pinned, isReplayable: false, reactivateRetired: true, automatic: false);
}
