// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Defines result-aware event seeding for coordination between seeding grains.
/// </summary>
/// <remarks>
/// This is a distinct additive grain interface so implementations compiled against <see cref="IEventSeeding"/> do
/// not gain a new abstract member. The original interface remains source and binary compatible.
/// </remarks>
public interface IResultAwareEventSeeding : IEventSeeding
{
    /// <summary>
    /// Seed events into the event store and report whether every entry was appended.
    /// </summary>
    /// <param name="entries">Collection of <see cref="SeedingEntry"/> to seed.</param>
    /// <returns>A <see cref="SeedingResult"/> telling whether every entry was appended.</returns>
    /// <remarks>
    /// Entries that were not appended are not recorded as seeded, so offering them again seeds them. The
    /// caller needs the outcome for exactly that reason: a caller that records the operation as done on
    /// behalf of this grain would otherwise never offer them again.
    /// </remarks>
    Task<SeedingResult> SeedWithResult(IEnumerable<SeedingEntry> entries);
}
