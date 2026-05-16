// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Integration.for_Webhooks.given;

public class webhook_states_after_registering(ChronicleFixture chronicleInProcessFixture)
    : all_dependencies(chronicleInProcessFixture)
{
    public IEnumerable<Concepts.Observation.Webhooks.WebhookDefinition> StoredWebhooks { get; private set; }

    protected async Task Register(params (WebhookId, WebhookTargetUrl, Action<IWebhookDefinitionBuilder>)[] registration)
    {
        foreach (var (id, targetUrl, configure) in registration)
        {
            await Webhooks.Register(id, targetUrl, configure);
        }

        var webhookReactor = await EventStore.Reactors.WaitForHandlerById(
            "$system.Cratis.Chronicle.Observation.Webhooks.WebhookReactor",
            TimeSpanFactory.FromSeconds(30));
        var systemStorage = GetSystemEventLogStorage();
        var tailSequenceNumber = (await systemStorage.GetTailSequenceNumber()).Value;
        await webhookReactor.WaitTillReachesEventSequenceNumber(tailSequenceNumber);

        // The webhook grain persists definitions to MongoDB asynchronously via a reminder.
        // Poll until the definitions have been written.
        using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
        while (true)
        {
            StoredWebhooks = await EventStoreStorage.Webhooks.GetAll();
            if (StoredWebhooks.Any())
            {
                break;
            }

            await Task.Delay(50, cts.Token);
        }
    }
}
