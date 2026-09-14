// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Reactors.Kernel;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles EventStoreAdded and NamespaceAdded events to ensure reactors are discovered and registered.
/// </summary>
/// <param name="reactors">The <see cref="IReactors"/> to use for discovering and registering reactors.</param>
/// <param name="eventTypes">The <see cref="IEventTypes"/> to use for discovering and registering event types.</param>
/// <param name="storage">The <see cref="IStorage"/> to check whether a namespace already holds data.</param>
[Reactor(eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: true)]
public class ReactorsReactor(IReactors reactors, IEventTypes eventTypes, IStorage storage) : Reactor
{
    /// <summary>
    /// Handles the addition of an event store.
    /// </summary>
    /// <param name="event">The event containing the event store information.</param>
    /// <param name="context">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task EventStoreAdded(EventStoreAdded @event, EventContext context)
    {
        await eventTypes.DiscoverAndRegister(@event.EventStore);
        await reactors.DiscoverAndRegister(@event.EventStore, EventStoreNamespaceName.Default);
    }

    /// <summary>
    /// Handles the addition of a namespace.
    /// </summary>
    /// <param name="event">The event containing the namespace information.</param>
    /// <param name="context">The context of the event.</param>
    /// <returns>Await Task.</returns>
    /// <remarks>
    /// A namespace is brand new the instant it is added, so it never has data at this point - registering reactors
    /// for it here would materialize its storage for a namespace that may never receive a single event. Reactor
    /// registration for a namespace that does go on to receive data happens the next time the server rehydrates -
    /// <see cref="Orleans.Hosting.ChronicleServerStartupTask"/> discovers namespaces with data and registers
    /// reactors for them then.
    /// </remarks>
    public async Task NamespaceAdded(NamespaceAdded @event, EventContext context)
    {
        var namespaceStorage = storage.GetEventStore(@event.EventStore).GetNamespace(@event.Namespace);
        if (!await namespaceStorage.HasData())
        {
            return;
        }

        await reactors.DiscoverAndRegister(@event.EventStore, @event.Namespace);
    }
}
