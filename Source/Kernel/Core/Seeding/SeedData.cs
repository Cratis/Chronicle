// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Represents the read model for the events seeded into an event store.
/// </summary>
/// <param name="ByEventType">Seed entries grouped by event type.</param>
/// <param name="ByEventSource">Seed entries grouped by event source.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventSeeding)]
public record SeedData(
    IEnumerable<Contracts.Seeding.EventTypeSeedEntries> ByEventType,
    IEnumerable<Contracts.Seeding.EventSourceSeedEntries> ByEventSource)
{
    /// <summary>
    /// Gets the seed data that applies to every namespace in an event store.
    /// </summary>
    /// <param name="eventStore">The event store to get seed data for.</param>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the seeding grain with.</param>
    /// <returns>The global seed data.</returns>
    internal static Task<SeedData> GetGlobalSeedData(EventStoreName eventStore, IGrainFactory grainFactory) =>
        SeedingConverters.Read(grainFactory, EventSeedingKey.ForGlobal(eventStore));

    /// <summary>
    /// Gets the seed data scoped to one namespace within an event store.
    /// </summary>
    /// <param name="eventStore">The event store to get seed data for.</param>
    /// <param name="namespace">The namespace to get seed data for.</param>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the seeding grain with.</param>
    /// <returns>The namespaced seed data.</returns>
    internal static Task<SeedData> GetNamespaceSeedData(EventStoreName eventStore, EventStoreNamespaceName @namespace, IGrainFactory grainFactory) =>
        SeedingConverters.Read(grainFactory, EventSeedingKey.ForNamespace(eventStore, @namespace));
}
