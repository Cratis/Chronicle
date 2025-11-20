// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Seeding;

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
}
