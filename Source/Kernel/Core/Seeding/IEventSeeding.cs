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
    /// <returns>A <see cref="SeedingResult"/> telling whether every entry was appended.</returns>
    /// <remarks>
    /// Entries that were not appended are not recorded as seeded, so offering them again seeds them. The
    /// caller needs the outcome for exactly that reason: a caller that records the operation as done on
    /// behalf of this grain would otherwise never offer them again.
    /// </remarks>
    Task<SeedingResult> Seed(IEnumerable<SeedingEntry> entries);

    /// <summary>
    /// Get all seeded events organized by event type and event source.
    /// </summary>
    /// <returns>The <see cref="EventSeeds"/> containing entries organized by event type and event source.</returns>
    Task<EventSeeds> GetSeededEvents();
}
