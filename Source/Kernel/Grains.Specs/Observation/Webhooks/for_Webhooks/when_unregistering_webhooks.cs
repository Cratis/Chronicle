// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_Webhooks.when_unregistering_webhooks;

#pragma warning disable SA1649 // File name should match first type name

public class with_webhook_ids : given.a_webhooks_service_grain
{
    IWebhooksManager _grainManager;
    Contracts.Observation.Webhooks.RemoveWebhooks _request;

    void Establish()
    {
        _grainManager = Substitute.For<IWebhooksManager>();
        _grainFactory.GetGrain<IWebhooksManager>(Arg.Any<string>()).Returns(_grainManager);

        _request = new Contracts.Observation.Webhooks.RemoveWebhooks
        {
            EventStore = "test-event-store",
            Webhooks = ["webhook-1", "webhook-2"]
        };
    }

    async Task Because() => await _webhooks.Remove(_request);

    [Fact] void should_get_webhooks_manager_for_event_store() =>
        _grainFactory.Received(1).GetGrain<IWebhooksManager>(_request.EventStore);

    [Fact] void should_remove_webhooks_on_manager() =>
        _grainManager.Received().Remove(Arg.Any<WebhookId>());
}
