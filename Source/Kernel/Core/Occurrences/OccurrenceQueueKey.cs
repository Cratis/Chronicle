// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Resolves queues without reserving a tenant name for application scope.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace, or null for application scope.</param>
/// <param name="Trigger">The stable trigger.</param>
internal record OccurrenceQueueKey(string EventStore, string? Namespace, string Trigger)
{
    /// <summary>
    /// Resolves an unambiguous queue key.
    /// </summary>
    /// <param name="factory">The grain factory.</param>
    /// <param name="eventStore">The store.</param>
    /// <param name="namespace">The namespace or application scope.</param>
    /// <param name="trigger">The trigger identity.</param>
    /// <returns>The queue grain.</returns>
    internal static IOccurrenceQueue Resolve(IGrainFactory factory, EventStoreName eventStore, string? @namespace, string trigger) =>
        factory.GetGrain<IOccurrenceQueue>(JsonSerializer.Serialize(new OccurrenceQueueKey(eventStore.Value, @namespace, trigger)));

    /// <summary>
    /// Selects the storage scope without conflating application and namespace work.
    /// </summary>
    /// <param name="storage">The kernel storage.</param>
    /// <returns>The scoped infrastructure storage.</returns>
    internal IOccurrencesStorage Storage(IStorage storage)
    {
        var eventStore = storage.GetEventStore(new EventStoreName(EventStore));
        return Namespace is null ? eventStore.Occurrences : eventStore.GetNamespace(new EventStoreNamespaceName(Namespace)).Occurrences;
    }
}
