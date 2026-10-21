// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookReactor.when_changing_delivery;

public class with_a_new_policy_and_pins : given.a_webhook_reactor
{
    WebhookDefinition _definition;
    GenerationDeliverySetForWebhook _event;
    EventContext _context;

    void Establish()
    {
        _definition = new("webhook", WebhookOwner.Client, "log", [new EventType("customer", 1)], new("https://example.com/webhook", WebhookAuthorization.None, new Dictionary<string, string>()));
        _webhooksManager.GetWebhookDefinitions().Returns([_definition]);
        _context = EventContext.EmptyWithEventSourceId("webhook") with { EventStore = "store" };
        _event = new(EventGenerationDelivery.Pinned, [new EventType("customer", 3)]);
    }

    async Task Because() => await _reactor.GenerationDeliverySet(_event, _context);
    [Fact] async Task should_update_the_policy_and_pins_together() => await _webhooksManager.Received(1).Update(Arg.Is<WebhookDefinition>(_ => _.GenerationDelivery == EventGenerationDelivery.Pinned && _.EventTypes.Single() == new EventType("customer", 3)));
    [Fact] async Task should_preserve_the_target() => await _webhooksManager.Received(1).Update(Arg.Is<WebhookDefinition>(_ => _.Target == _definition.Target));
}
