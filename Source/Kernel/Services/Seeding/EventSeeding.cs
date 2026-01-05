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
        // Group entries by whether they are global or namespace-specific
        var globalEntries = request.Entries.Where(e => e.IsGlobal).ToList();
        var namespaceEntries = request.Entries.Where(e => !e.IsGlobal).ToList();

        // Seed global entries to the global grain
        if (globalEntries.Count > 0)
        {
            var globalKey = EventSeedingKey.ForGlobal(request.EventStore);
            var globalGrain = grainFactory.GetGrain<Grains.Seeding.IEventSeeding>(globalKey.ToString());

            var entries = globalEntries.Select(e => new Grains.Seeding.SeedingEntry(
                e.EventSourceId,
                e.EventTypeId,
                e.Content,
                e.IsGlobal,
                e.TargetNamespace)).ToArray();

            await globalGrain.Seed(entries);
        }

        // Seed namespace-specific entries grouped by target namespace
        var entriesByNamespace = namespaceEntries.GroupBy(e => e.TargetNamespace);
        foreach (var group in entriesByNamespace)
        {
            var targetNamespace = string.IsNullOrEmpty(group.Key) ? request.Namespace : group.Key;
            var key = EventSeedingKey.ForNamespace(request.EventStore, targetNamespace);
            var grain = grainFactory.GetGrain<Grains.Seeding.IEventSeeding>(key.ToString());

            var entries = group.Select(e => new Grains.Seeding.SeedingEntry(
                e.EventSourceId,
                e.EventTypeId,
                e.Content,
                e.IsGlobal,
                targetNamespace)).ToArray();

            await grain.Seed(entries);
        }
    }

    /// <inheritdoc/>
    public async Task<SeedDataResponse> GetGlobalSeedData(GetSeedDataRequest request, CallContext context = default)
    {
        var key = EventSeedingKey.ForGlobal(request.EventStore);
        var grain = grainFactory.GetGrain<Grains.Seeding.IEventSeeding>(key.ToString());
        var entries = await grain.GetSeededEvents();

        return new SeedDataResponse
        {
            Entries = entries.Select(e => new Contracts.Seeding.SeedingEntry
            {
                EventSourceId = e.EventSourceId.Value,
                EventTypeId = e.EventTypeId.Value,
                Content = e.Content,
                IsGlobal = e.IsGlobal,
                TargetNamespace = e.TargetNamespace.Value
            }).ToList()
        };
    }

    /// <inheritdoc/>
    public async Task<SeedDataResponse> GetNamespaceSeedData(GetSeedDataRequest request, CallContext context = default)
    {
        var key = EventSeedingKey.ForNamespace(request.EventStore, request.Namespace);
        var grain = grainFactory.GetGrain<Grains.Seeding.IEventSeeding>(key.ToString());
        var entries = await grain.GetSeededEvents();

        return new SeedDataResponse
        {
            Entries = entries.Select(e => new Contracts.Seeding.SeedingEntry
            {
                EventSourceId = e.EventSourceId.Value,
                EventTypeId = e.EventTypeId.Value,
                Content = e.Content,
                IsGlobal = e.IsGlobal,
                TargetNamespace = e.TargetNamespace.Value
            }).ToList()
        };
    }
}
