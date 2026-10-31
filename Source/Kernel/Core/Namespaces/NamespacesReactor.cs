// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Seeding;
using Microsoft.Extensions.Logging;

#pragma warning disable IDE0060 // Remove unused parameter

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents a reactor that handles namespace events.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="patternCapture">The <see cref="IPatternCapture"/> for observing the new namespace's events.</param>
/// <param name="logger">The logger.</param>
[Reactor(eventSequence: WellKnownEventSequences.System, systemEventStoreOnly: true)]
public class NamespacesReactor(IGrainFactory grainFactory, IPatternCapture patternCapture, ILogger<NamespacesReactor> logger) : Reactor
{
    /// <summary>
    /// Handles the addition of a namespace by subscribing pattern capture for it and applying any existing global
    /// seed data to it.
    /// </summary>
    /// <param name="event">The event containing the namespace information.</param>
    /// <param name="eventContext">The context of the event.</param>
    /// <returns>Await Task.</returns>
    /// <exception cref="EventSeedingIncomplete">Thrown when at least one global seed entry was not appended to the namespace.</exception>
    public async Task Added(NamespaceAdded @event, EventContext eventContext)
    {
        try
        {
            await patternCapture.Subscribe(@event.EventStore, @event.Namespace);
        }
        catch (Exception exception)
        {
            logger.FailedSubscribingPatternCapture(exception, @event.EventStore, @event.Namespace);
        }

        var globalKey = EventSeedingKey.ForGlobal(@event.EventStore);
        var globalGrain = grainFactory.GetGrain<IResultAwareEventSeeding>(globalKey.ToString());
        var seeds = await globalGrain.GetSeededEvents();

        if (seeds.ByEventSource.Count == 0)
        {
            return;
        }

        var entries = seeds.ByEventSource
            .SelectMany(kvp => kvp.Value)
            .Select(e => new SeedingEntry(e.EventSourceId, e.EventTypeId, e.Content, e.Tags?.Select(t => new Tag(t)), e.EventSourceType, e.EventStreamType, e.EventStreamId))
            .ToArray();

        if (entries.Length > 0)
        {
            var namespaceKey = EventSeedingKey.ForNamespace(@event.EventStore, @event.Namespace);
            var nsGrain = grainFactory.GetGrain<IResultAwareEventSeeding>(namespaceKey.ToString());
            var result = await nsGrain.SeedWithResult(entries);
            if (!result.AllEntriesSeeded)
            {
                throw new EventSeedingIncomplete(@event.EventStore, @event.Namespace);
            }
        }
    }
}
