// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Api.Seeding;

/// <summary>
/// Represents a seeded event entry for API usage.
/// </summary>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="Content">The JSON content of the event.</param>
/// <param name="IsGlobal">Whether this seed data is global (applies to all namespaces).</param>
/// <param name="TargetNamespace">The specific namespace this seed data applies to, if not global.</param>
[ReadModel]
public record SeedDataEntry(
    string EventSourceId,
    string EventTypeId,
    string Content,
    bool IsGlobal,
    string TargetNamespace)
{
    /// <summary>
    /// Gets all global seed data for an event store.
    /// </summary>
    /// <param name="eventSeeding">The event seeding contract.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <returns>Collection of global seed data entries.</returns>
    public static async Task<IEnumerable<SeedDataEntry>> AllGlobalSeedData(IEventSeeding eventSeeding, string eventStore)
    {
        var response = await eventSeeding.GetGlobalSeedData(new GetSeedDataRequest
        {
            EventStore = eventStore
        });

        return response.Entries.Select(e => new SeedDataEntry(
            e.EventSourceId,
            e.EventTypeId,
            e.Content,
            e.IsGlobal,
            e.TargetNamespace));
    }

    /// <summary>
    /// Gets all namespace-specific seed data for an event store.
    /// </summary>
    /// <param name="eventSeeding">The event seeding contract.</param>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <returns>Collection of namespace-specific seed data entries.</returns>
    public static async Task<IEnumerable<SeedDataEntry>> AllSeedDataForNamespace(IEventSeeding eventSeeding, string eventStore, string @namespace)
    {
        var response = await eventSeeding.GetNamespaceSeedData(new GetSeedDataRequest
        {
            EventStore = eventStore,
            Namespace = @namespace
        });

        return response.Entries.Select(e => new SeedDataEntry(
            e.EventSourceId,
            e.EventTypeId,
            e.Content,
            e.IsGlobal,
            e.TargetNamespace));
    }
}
