// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ContractWebhookDefinition = Cratis.Chronicle.Contracts.Observation.Webhooks.WebhookDefinition;
using DomainWebhookDefinition = Cratis.Chronicle.Concepts.Observation.Webhooks.WebhookDefinition;

namespace Cratis.Chronicle.Grains.Observation.Webhooks.for_Webhooks.when_registering_webhooks;

#pragma warning disable SA1649 // File name should match first type name

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

    async Task Because() => await _webhooks.Register(_request);

    [Fact] void should_get_webhooks_manager_for_event_store() =>
        _grainFactory.Received(1).GetGrain<IWebhooksManager>(_request.EventStore);

    [Fact] void should_register_webhook_on_manager() =>
        _grainManager.Received(1).Register(Arg.Is<DomainWebhookDefinition[]>(w => w.Length == 1));
}
