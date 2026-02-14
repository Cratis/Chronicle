// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grains.Namespaces;

namespace Cratis.Chronicle.Observation.Reactors.Kernel;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles EventStoreAdded and NamespaceAdded events to ensure reactors are discovered and registered.
/// </summary>
/// <param name="reactors">The <see cref="IReactors"/> to use for discovering and registering reactors.</param>
[Reactor(eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: true)]
public class ReactorsReactor(IReactors reactors) : Reactor
{
    /// <summary>
    /// Handles the addition of an event store.
    /// </summary>
    /// <param name="event">The event containing the event store information.</param>
    /// <param name="context">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public Task EventStoreAdded(EventStoreAdded @event, EventContext context)
        => reactors.DiscoverAndRegister(@event.EventStore, EventStoreNamespaceName.Default);

    /// <summary>
    /// Handles the addition of a namespace.
    /// </summary>
    /// <param name="event">The event containing the namespace information.</param>
    /// <param name="context">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public Task NamespaceAdded(NamespaceAdded @event, EventContext context)
        => reactors.DiscoverAndRegister(@event.EventStore, @event.Namespace);
}
