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

        // The EventStoreSubscriptionsReactor is a kernel-side system reactor registered asynchronously
        // via ReactorsReactor processing EventStoreAdded. Wait until it is available.
        IReactorHandler? subscriptionsReactor = null;
        var deadline = DateTimeOffset.UtcNow.Add(TimeSpanFactory.FromSeconds(60));
        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                subscriptionsReactor = EventStore.Reactors.GetHandlerById("$system.Cratis.Chronicle.Observation.EventStoreSubscriptions.EventStoreSubscriptionsReactor");
                break;
            }
            catch (UnknownReactorId)
            {
                await Task.Delay(100);
            }
        }

        subscriptionsReactor.ShouldNotBeNull();
        var systemStorage = GetSystemEventLogStorage();
        var tailSequenceNumber = (await systemStorage.GetTailSequenceNumber()).Value;
        await subscriptionsReactor.WaitTillReachesEventSequenceNumber(tailSequenceNumber);

        StoredSubscriptions = await EventStoreStorage.EventStoreSubscriptions.GetAll();
    }
}
