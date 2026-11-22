// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Webhooks;

namespace Cratis.Chronicle.Integration.for_Webhooks.given;

public class webhook_states_after_registering(ChronicleFixture chronicleInProcessFixture)
    : all_dependencies(chronicleInProcessFixture)
{
    public IEnumerable<WebhookDefinition> StoredWebhooks { get; private set; }

    protected async Task Register(params (WebhookId, WebhookTargetUrl, Action<IWebhookDefinitionBuilder>)[] registration)
    {
        foreach (var (id, targetUrl, configure) in registration)
        {
            await Webhooks.Register(id, targetUrl, configure);
        }

        var webhookReactor = await EventStore.Reactors.WaitForHandlerById(
            "$system.Cratis.Chronicle.Observation.Webhooks.WebhookReactor",
            TimeSpanFactory.FromSeconds(30));

        var systemLog = EventStore.GetEventSequence(EventSequenceId.System);
        var tailSequenceNumber = (await systemLog.GetTailSequenceNumber()).Value;
        await webhookReactor.WaitTillReachesEventSequenceNumber(tailSequenceNumber);

        // Poll until the definitions have been persisted server-side.
        using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());

        while (true)
        {
            StoredWebhooks = await Webhooks.GetAll();

            if (StoredWebhooks.Any())
            {
                break;
            }

            await Task.Delay(50, cts.Token);
        }
    }
}
