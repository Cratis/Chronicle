// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.given;

public class subscription_states_after_subscribing(ChronicleFixture chronicleInProcessFixture)
    : Specification(chronicleInProcessFixture)
{
    public IEnumerable<EventStoreSubscriptionDefinition> StoredSubscriptions { get; private set; } = [];

    protected async Task Subscribe(params (EventStoreSubscriptionId Id, string SourceEventStore, Action<IEventStoreSubscriptionBuilder>? Configure)[] subscriptions)
    {
        foreach (var (id, sourceEventStore, configure) in subscriptions)
        {
            await EventStore.Subscriptions.Subscribe(id, sourceEventStore, configure);
        }

        // Poll until the subscriptions appear in storage. Waiting on the reactor's sequence
        // number is unreliable on SQL backends because the internal subscriptions reactor
        // may process events slower than the polling period.
        using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
        while (true)
        {
            StoredSubscriptions = await EventStore.Subscriptions.GetAll();
            if (StoredSubscriptions.Any())
            {
                break;
            }

            await Task.Delay(100, cts.Token);
        }
    }
}
