// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.InProcess.Integration.for_EventStoreSubscriptions.given;

public class subscription_states_after_subscribing(ChronicleInProcessFixture chronicleInProcessFixture)
    : Specification(chronicleInProcessFixture)
{
    public IEnumerable<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition> StoredSubscriptions { get; private set; } = [];

    protected async Task Subscribe(params (EventStoreSubscriptionId, string, Action<IEventStoreSubscriptionBuilder>?)[] subscriptions)
    {
        foreach (var (id, sourceEventStore, configure) in subscriptions)
        {
            await EventStore.Subscriptions.Subscribe(id, sourceEventStore, configure);
        }

        var subscriptionsReactor = await EventStore.Reactors.WaitForHandlerById(
            "$system.Cratis.Chronicle.Observation.EventStoreSubscriptions.EventStoreSubscriptionsReactor",
            TimeSpanFactory.FromSeconds(60));
        var systemStorage = GetSystemEventLogStorage();
        var tailSequenceNumber = (await systemStorage.GetTailSequenceNumber()).Value;
        await subscriptionsReactor.WaitTillReachesEventSequenceNumber(tailSequenceNumber);

        StoredSubscriptions = await EventStoreStorage.EventStoreSubscriptions.GetAll();
    }
}
