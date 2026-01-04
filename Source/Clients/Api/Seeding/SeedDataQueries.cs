// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Api.Seeding;

/// <summary>
/// Represents the API for querying seed data.
/// </summary>
[Route("/api/event-stores/{eventStore}/seed-data")]
public class SeedDataQueries : ControllerBase
{
    readonly IEventSeeding _eventSeeding;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeedDataQueries"/> class.
    /// </summary>
    /// <param name="eventSeeding">The event seeding contract.</param>
    internal SeedDataQueries(IEventSeeding eventSeeding)
    {
        _eventSeeding = eventSeeding;
    }

    /// <summary>
    /// Get global seed data for an event store.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <returns>Collection of global seed data entries.</returns>
    [HttpGet("global")]
    public async Task<IEnumerable<SeedDataEntry>> GetGlobalSeedData(string eventStore)
    {
        var response = await _eventSeeding.GetGlobalSeedData(new GetSeedDataRequest
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
    /// Get namespace-specific seed data.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="namespace">The namespace name.</param>
    /// <returns>Collection of namespace-specific seed data entries.</returns>
    [HttpGet("namespace/{namespace}")]
    public async Task<IEnumerable<SeedDataEntry>> GetNamespaceSeedData(string eventStore, string @namespace)
    {
        var response = await _eventSeeding.GetNamespaceSeedData(new GetSeedDataRequest
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
