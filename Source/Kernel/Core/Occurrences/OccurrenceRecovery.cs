// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Recovers durable queues independently of event-log data and silo-local activations.
/// </summary>
public static class OccurrenceRecovery
{
    /// <summary>
    /// Wakes every trigger with unfinished work in one scope.
    /// </summary>
    /// <param name="factory">The grain factory.</param>
    /// <param name="storage">The scoped occurrence store.</param>
    /// <param name="eventStore">The store.</param>
    /// <param name="namespace">The namespace or null for application scope.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task Rehydrate(IGrainFactory factory, IOccurrencesStorage storage, EventStoreName eventStore, string? @namespace)
    {
        foreach (var trigger in await storage.GetPendingTriggers())
        {
            await OccurrenceQueueKey.Resolve(factory, eventStore, @namespace, trigger.Value).Wake();
        }
    }

    /// <summary>
    /// Expires one connection's leases in every namespace and application scope.
    /// </summary>
    /// <param name="storage">The kernel storage.</param>
    /// <param name="worker">The disconnected connection identity.</param>
    /// <param name="now">The kernel instant.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task ExpireLeases(IStorage storage, string worker, DateTimeOffset now)
    {
        foreach (var name in await storage.GetEventStores())
        {
            var store = storage.GetEventStore(name);
            await store.Occurrences.ExpireLeases(worker, now);
            foreach (var state in await store.Namespaces.GetAll())
            {
                await store.GetNamespace(state.Name).Occurrences.ExpireLeases(worker, now);
            }
        }
    }
}
