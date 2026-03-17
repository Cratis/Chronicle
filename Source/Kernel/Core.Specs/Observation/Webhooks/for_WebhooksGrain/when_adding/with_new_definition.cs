// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhooksGrain.when_adding;

public class with_new_definition : given.a_webhooks_manager_grain
{
    WebhookDefinition _definition;

    void Establish()
    {
        var webhookId = new WebhookId(Guid.NewGuid().ToString());
        _definition = new WebhookDefinition(
            webhookId,
            WebhookOwner.Client,
            EventSequenceId.Log,
            [],
            new WebhookTarget(
                new WebhookTargetUrl("https://example.com/webhook"),
                WebhookAuthorization.None,
                new Dictionary<string, string>()));

        _webhookGrain.GetDefinition().Returns(_definition);
    }

    async Task Because() => await _grain.Add(_definition);

    [Fact] void should_add_webhook_to_state() => _stateStorage.State.Webhooks.ShouldContain(_definition);
}
