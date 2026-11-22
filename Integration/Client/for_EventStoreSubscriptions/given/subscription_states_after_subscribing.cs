// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.given;

public class subscription_states_after_subscribing(ChronicleFixture chronicleInProcessFixture)
    : Specification(chronicleInProcessFixture)
{
    readonly HashSet<string> _subscribedIds = [];

    public IEnumerable<EventStoreSubscriptionDefinition> StoredSubscriptions { get; private set; } = [];

    protected async Task Subscribe(params (EventStoreSubscriptionId Id, string SourceEventStore, Action<IEventStoreSubscriptionBuilder>? Configure)[] subscriptions)
    {
        foreach (var (id, sourceEventStore, configure) in subscriptions)
        {
            await EventStore.Subscriptions.Subscribe(id, sourceEventStore, configure);
            _subscribedIds.Add(id.Value);
        }

        // Poll until every subscription this specification made appears in storage. Waiting on the
        // reactor's sequence number is unreliable on SQL backends because the internal subscriptions
        // reactor may process events slower than the polling period.
        using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
        while (true)
        {
            StoredSubscriptions = await GetOwnStoredSubscriptions();
            if (_subscribedIds.All(id => StoredSubscriptions.Any(_ => _.Id.Value == id)))
            {
                break;
            }

            await Task.Delay(100, cts.Token);
        }
    }

    protected async Task<IEnumerable<EventStoreSubscriptionDefinition>> GetOwnStoredSubscriptions()
    {
        // Only the subscriptions this specification made are its subject. The shared kernel can still hold
        // a subscription another class in the collection made, until its reset has settled; counting those
        // would make these specifications depend on class order. A duplicate of one of our own ids still shows.
        var all = await EventStore.Subscriptions.GetAll();
        return all.Where(_ => _subscribedIds.Contains(_.Id.Value)).ToArray();
    }
}
