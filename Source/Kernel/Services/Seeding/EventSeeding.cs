// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Contracts.Seeding;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Seeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeeding"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
internal sealed class EventSeeding(IGrainFactory grainFactory) : IEventSeeding
{
    /// <inheritdoc/>
    public async Task Seed(SeedRequest request, CallContext context = default)
    {
        // Seed global entries to the global grain
        var globalEntries = new List<Contracts.Seeding.SeedingEntry>();

        // Collect all global entries from both ByEventType and ByEventSource
        foreach (var eventTypeGroup in request.GlobalByEventType)
        {
            globalEntries.AddRange(eventTypeGroup.Entries);
        }

        foreach (var eventSourceGroup in request.GlobalByEventSource)
        {
            globalEntries.AddRange(eventSourceGroup.Entries);
        }

        // Deduplicate global entries (same entry might be in both ByEventType and ByEventSource)
        globalEntries = globalEntries
            .GroupBy(e => new { e.EventSourceId, e.EventTypeId, e.Content })
            .Select(g => g.First())
            .ToList();

        if (globalEntries.Count > 0)
        {
            var globalKey = EventSeedingKey.ForGlobal(request.EventStore);
            var globalGrain = grainFactory.GetGrain<Grains.Seeding.IEventSeeding>(globalKey.ToString());

            var entries = globalEntries.Select(e => new Grains.Seeding.SeedingEntry(
                e.EventSourceId,
                e.EventTypeId,
                e.Content,
                e.Tags?.Select(t => new Concepts.Events.Tag(t)).ToArray())).ToArray();

            await globalGrain.Seed(entries);
        }

        // Seed namespace-specific entries
        foreach (var namespacedGroup in request.NamespacedEntries)
        {
            var namespacedEntries = new List<Contracts.Seeding.SeedingEntry>();

            // Collect all entries from both ByEventType and ByEventSource
            foreach (var eventTypeGroup in namespacedGroup.ByEventType)
            {
                namespacedEntries.AddRange(eventTypeGroup.Entries);
            }

            foreach (var eventSourceGroup in namespacedGroup.ByEventSource)
            {
                namespacedEntries.AddRange(eventSourceGroup.Entries);
            }

            // Deduplicate entries
            namespacedEntries = namespacedEntries
                .GroupBy(e => new { e.EventSourceId, e.EventTypeId, e.Content })
                .Select(g => g.First())
                .ToList();

            if (namespacedEntries.Count > 0)
            {
                var key = EventSeedingKey.ForNamespace(request.EventStore, namespacedGroup.Namespace);
                var grain = grainFactory.GetGrain<Grains.Seeding.IEventSeeding>(key.ToString());

                var entries = namespacedEntries.Select(e => new Grains.Seeding.SeedingEntry(
                    e.EventSourceId,
                    e.EventTypeId,
                    e.Content,
                    e.Tags?.Select(t => new Concepts.Events.Tag(t)).ToArray())).ToArray();

                await grain.Seed(entries);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<SeedDataResponse> GetGlobalSeedData(GetSeedDataRequest request, CallContext context = default)
    {
        var key = EventSeedingKey.ForGlobal(request.EventStore);
        var grain = grainFactory.GetGrain<Grains.Seeding.IEventSeeding>(key.ToString());
        var seeds = await grain.GetSeededEvents();

        return MapToResponse(seeds);
    }

    /// <inheritdoc/>
    public async Task<SeedDataResponse> GetNamespaceSeedData(GetSeedDataRequest request, CallContext context = default)
    {
        var key = EventSeedingKey.ForNamespace(request.EventStore, request.Namespace);
        var grain = grainFactory.GetGrain<Grains.Seeding.IEventSeeding>(key.ToString());
        var seeds = await grain.GetSeededEvents();

        return MapToResponse(seeds);
    }

    static SeedDataResponse MapToResponse(Storage.Seeding.EventSeeds seeds)
    {
        var response = new SeedDataResponse();

        foreach (var (eventTypeId, entries) in seeds.ByEventType)
        {
            response.ByEventType.Add(new EventTypeSeedEntries
            {
                EventTypeId = eventTypeId.Value,
                Entries = entries.Select(e => new Contracts.Seeding.SeedingEntry
                {
                    EventSourceId = e.EventSourceId.Value,
                    EventTypeId = e.EventTypeId.Value,
                    Content = e.Content
                }).ToList()
            });
        }

        foreach (var (eventSourceId, entries) in seeds.ByEventSource)
        {
            response.ByEventSource.Add(new EventSourceSeedEntries
            {
                EventSourceId = eventSourceId.Value,
                Entries = entries.Select(e => new Contracts.Seeding.SeedingEntry
                {
                    EventSourceId = e.EventSourceId.Value,
                    EventTypeId = e.EventTypeId.Value,
                    Content = e.Content
                }).ToList()
            });
        }

        return response;
    }
}
