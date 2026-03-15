// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Seeding;

#pragma warning disable IDE0060 // Remove unused parameter

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents a reactor that handles namespace events.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
[Reactor(eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: true)]
public class NamespacesReactor(IGrainFactory grainFactory) : Reactor
{
    /// <summary>
    /// Handles the addition of a namespace by applying any existing global seed data to it.
    /// </summary>
    /// <param name="event">The event containing the namespace information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    public async Task Added(NamespaceAdded @event, EventContext eventContext)
    {
        var globalKey = EventSeedingKey.ForGlobal(@event.EventStore);
        var globalGrain = grainFactory.GetGrain<IEventSeeding>(globalKey.ToString());
        var seeds = await globalGrain.GetSeededEvents();

        if (seeds.ByEventSource.Count == 0)
        {
            return;
        }

        var entries = seeds.ByEventSource
            .SelectMany(kvp => kvp.Value)
            .GroupBy(e => new { e.EventSourceId, e.EventTypeId, e.Content })
            .Select(g => g.First())
            .Select(e => new SeedingEntry(e.EventSourceId, e.EventTypeId, e.Content, e.Tags?.Select(t => new Tag(t))))
            .ToArray();

        if (entries.Length > 0)
        {
            var namespaceKey = EventSeedingKey.ForNamespace(@event.EventStore, @event.Namespace);
            var nsGrain = grainFactory.GetGrain<IEventSeeding>(namespaceKey.ToString());
            await nsGrain.Seed(entries);
        }
    }
}
