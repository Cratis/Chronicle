// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookReactor;

public class when_adding_a_signed_webhook : given.a_webhook_reactor
{
    async Task Because() => await _reactor.Added(
        new(WebhookOwner.Client, "log", [], "https://example.com/webhook", new Dictionary<string, string>(), true, true) { Signing = WebhookSigningMode.Ed25519 },
        EventContext.EmptyWithEventSourceId("webhook") with { EventStore = "store" });

    [Fact] async Task should_store_the_signing_policy() => await _webhooksManager.Received(1).Add(Arg.Is<WebhookDefinition>(_ => _.Signing == WebhookSigningMode.Ed25519));
}
