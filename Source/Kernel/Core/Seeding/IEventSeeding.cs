// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Defines the event seeding grain.
/// </summary>
public interface IEventSeeding : IGrainWithStringKey
{
    /// <summary>
    /// Seed events into the event store.
    /// </summary>
    /// <param name="entries">Collection of <see cref="SeedingEntry"/> to seed.</param>
    /// <returns>Awaitable task.</returns>
    Task Seed(IEnumerable<SeedingEntry> entries);

    /// <summary>
    /// Get all seeded events organized by event type and event source.
    /// </summary>
    /// <returns>The <see cref="EventSeeds"/> containing entries organized by event type and event source.</returns>
    Task<EventSeeds> GetSeededEvents();
}
