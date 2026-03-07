// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhooksGrain.when_removing;

public class with_existing_webhook : given.a_webhooks_manager_grain
{
    WebhookDefinition _definition;
    WebhookId _webhookId;

    void Establish()
    {
        _webhookId = new WebhookId(Guid.NewGuid().ToString());
        _definition = new WebhookDefinition(
            _webhookId,
            WebhookOwner.Client,
            EventSequenceId.Log,
            [],
            new WebhookTarget(
                new WebhookTargetUrl("https://example.com/webhook"),
                WebhookAuthorization.None,
                new Dictionary<string, string>()));

        _stateStorage.State.Webhooks = [_definition];
        _webhookGrain.GetDefinition().Returns(_definition);
    }

    async Task Because() => await _grain.Remove(_webhookId);

    [Fact] void should_remove_webhook_from_state() => _stateStorage.State.Webhooks.ShouldNotContain(_definition);
    [Fact] void should_call_remove_on_webhook_grain() => _webhookGrain.Received(1).Remove();
}
