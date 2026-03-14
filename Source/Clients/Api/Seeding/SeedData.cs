// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Api.Seeding;

/// <summary>
/// Represents seed data organized by event type and event source.
/// </summary>
/// <param name="ByEventType">Seed entries grouped by event type.</param>
/// <param name="ByEventSource">Seed entries grouped by event source.</param>
[ReadModel]
public record SeedData(
    IEnumerable<EventTypeSeedGroup> ByEventType,
    IEnumerable<EventSourceSeedGroup> ByEventSource)
{
    /// <summary>
    /// Gets all global seed data for an event store.
    /// </summary>
    /// <param name="eventSeeding">The event seeding contract.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <returns>Global seed data organized by event type and event source.</returns>
    internal static async Task<SeedData> GlobalSeedData(IEventSeeding eventSeeding, string eventStore)
    {
        var response = await eventSeeding.GetGlobalSeedData(new GetSeedDataRequest
        {
            EventStore = eventStore
        });

        return new(
            response.ByEventType.Select(g => new EventTypeSeedGroup(
                g.EventTypeId,
                g.Entries.Select(e => new SeedEntry(e.EventSourceId, e.EventTypeId, e.Content)))),
            response.ByEventSource.Select(g => new EventSourceSeedGroup(
                g.EventSourceId,
                g.Entries.Select(e => new SeedEntry(e.EventSourceId, e.EventTypeId, e.Content)))));
    }

    /// <summary>
    /// Gets namespace-specific seed data for an event store.
    /// </summary>
    /// <param name="eventSeeding">The event seeding contract.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <returns>Namespace-specific seed data organized by event type and event source.</returns>
    internal static async Task<SeedData> NamespaceSeedData(IEventSeeding eventSeeding, string eventStore, string @namespace)
    {
        var response = await eventSeeding.GetNamespaceSeedData(new GetSeedDataRequest
        {
            EventStore = eventStore,
            Namespace = @namespace
        });

        return new(
            response.ByEventType.Select(g => new EventTypeSeedGroup(
                g.EventTypeId,
                g.Entries.Select(e => new SeedEntry(e.EventSourceId, e.EventTypeId, e.Content)))),
            response.ByEventSource.Select(g => new EventSourceSeedGroup(
                g.EventSourceId,
                g.Entries.Select(e => new SeedEntry(e.EventSourceId, e.EventTypeId, e.Content)))));
    }
}
