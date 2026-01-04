// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

namespace Cratis.Chronicle.Grains.Namespaces;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles namespace events.
/// </summary>
/// <param name="grainFactory">The grain factory for getting grains.</param>
[Reactor(eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: true)]
public class NamespacesReactor(IGrainFactory grainFactory) : Reactor
{
    /// <summary>
    /// Handles the addition of a namespace.
    /// </summary>
    /// <param name="event">The event containing the namespace information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Added(NamespaceAdded @event, EventContext eventContext)
    {
        // Get the global seed grain to retrieve global seed data
        var globalKey = EventSeedingKey.ForGlobal(@event.EventStore);
        var globalGrain = grainFactory.GetGrain<Seeding.IEventSeeding>(globalKey.ToString());

        // Get the namespace-specific seed grain
        var namespaceKey = EventSeedingKey.ForNamespace(@event.EventStore, @event.Namespace);
        var namespaceGrain = grainFactory.GetGrain<Seeding.IEventSeeding>(namespaceKey.ToString());

        // TODO: In the future, we need to retrieve global seed data and apply it to the new namespace
        // For now, the structure is in place for when the API endpoints are added to retrieve seed data
    }
}
