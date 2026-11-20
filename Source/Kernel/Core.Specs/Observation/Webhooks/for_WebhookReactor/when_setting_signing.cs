// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookReactor;

public class when_setting_signing : given.a_webhook_reactor
{
    WebhookDefinition _definition;
    EventContext _context;

    void Establish()
    {
        _definition = new Contracts.Observation.Webhooks.WebhookDefinition { Identifier = "webhook" }.ToChronicle();
        _webhooksManager.GetWebhookDefinitions().Returns([_definition]);
        _context = EventContext.EmptyWithEventSourceId("webhook") with { EventStore = "store" };
    }

    async Task Because() => await _reactor.SigningSet(new(WebhookSigningMode.Ed25519), _context);

    [Fact] async Task should_update_only_signing() => await _webhooksManager.Received(1).Update(_definition with { Signing = WebhookSigningMode.Ed25519 });
}
