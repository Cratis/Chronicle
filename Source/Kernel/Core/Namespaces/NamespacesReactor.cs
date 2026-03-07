// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Observation.Reactors.Kernel;

namespace Cratis.Chronicle.Namespaces;

#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// Represents a reactor that handles namespace events.
/// </summary>
[Reactor(eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: true)]
public class NamespacesReactor : Reactor
{
    /// <summary>
    /// Handles the addition of a namespace.
    /// </summary>
    /// <param name="event">The event containing the namespace information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public Task Added(NamespaceAdded @event, EventContext eventContext)
    {
        // TODO: In the future, we need to retrieve global seed data and apply it to the new namespace
        // When implemented, we will need to use IGrainFactory to:
        // 1. Get the global seed grain using EventSeedingKey.ForGlobal(@event.EventStore)
        // 2. Get the namespace-specific seed grain using EventSeedingKey.ForNamespace(@event.EventStore, @event.Namespace)
        // 3. Retrieve global seed data and apply it to the new namespace
        return Task.CompletedTask;
    }
}
