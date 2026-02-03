// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.InProcess.Integration.for_Webhooks.given;

public class webhook_states_after_registering(ChronicleInProcessFixture chronicleInProcessFixture)
    : all_dependencies(chronicleInProcessFixture)
{
    public IEnumerable<Cratis.Chronicle.Concepts.Observation.Webhooks.WebhookDefinition> StoredWebhooks { get; private set; }

    protected async Task Register(params (WebhookId, WebhookTargetUrl, Action<IWebhookDefinitionBuilder>)[] registration)
    {
        foreach (var (id, targetUrl, configure) in registration)
        {
            await Webhooks.Register(id, targetUrl, configure);
        }

        StoredWebhooks = await EventStoreStorage.Webhooks.GetAll();
    }
}