// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ContractWebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;
using DomainWebhookDefinition = Cratis.Chronicle.Concepts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_Webhooks.when_registering_webhooks;

public class with_valid_webhook_definition : given.a_webhooks_service_grain
{
    IWebhooksManager _grainManager;
    Contracts.Observation.Webhooks.AddWebhooks _request;

    void Establish()
    {
        _grainManager = Substitute.For<IWebhooksManager>();
        _grainFactory.GetGrain<IWebhooksManager>(Arg.Any<string>()).Returns(_grainManager);

        _request = new Contracts.Observation.Webhooks.AddWebhooks
        {
            EventStore = "test-event-store",
            Webhooks =
            [
                new ContractWebhookDefinition
                {
                    Identifier = "test-webhook",
                    IsActive = true
                }
            ]
        };
    }

    async Task Because() => await _webhooks.Add(_request);

    [Fact] void should_get_webhooks_manager_for_event_store() =>
        _grainFactory.Received(1).GetGrain<IWebhooksManager>(_request.EventStore);

    [Fact] void should_add_webhook_to_manager() =>
        _grainManager.Received().Add(Arg.Any<DomainWebhookDefinition>());
}
