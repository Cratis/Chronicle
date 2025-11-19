// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSeeding;

/// <summary>
/// Defines the storage provider for event seeding.
/// </summary>
public interface IEventSeedingStorage
{
    /// <summary>
    /// Get the seeding data.
    /// </summary>
    /// <returns><see cref="EventSeedingData"/>.</returns>
    Task<EventSeedingData> Get();

    /// <summary>
    /// Save the seeding data.
    /// </summary>
    /// <param name="data"><see cref="EventSeedingData"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(EventSeedingData data);
}
