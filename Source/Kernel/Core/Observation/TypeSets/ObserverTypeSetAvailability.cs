// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.TypeSets;

/// <summary>
/// Reports whether this kernel can manage an event store's observer type sets.
/// </summary>
internal static class ObserverTypeSetAvailability
{
    /// <summary>
    /// Gets availability without starting any operation.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="storage">The event store storage.</param>
    /// <returns>The inert operation outcome.</returns>
    public static async Task<ObserverTypeSetOperationOutcome> Get(string eventStore, IStorage storage) =>
        await storage.GetEventStore(eventStore).AreObserversInitialized()
            ? ObserverTypeSetOperationOutcome.Unavailable
            : ObserverTypeSetOperationOutcome.NotInitialized;
}
