// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Seeding;

/// <summary>
/// Defines the storage provider for event seeding.
/// </summary>
public interface IEventSeedingStorage
{
    /// <summary>
    /// Get the seeding data.
    /// </summary>
    /// <returns><see cref="EventSeeds"/>.</returns>
    Task<EventSeeds> Get();

    /// <summary>
    /// Save the seeding data.
    /// </summary>
    /// <param name="data"><see cref="EventSeeds"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(EventSeeds data);
}
