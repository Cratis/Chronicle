// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Resolves observer routing from the same accepted event identities the owning invoker dispatches.
/// </summary>
internal static class ObserverEventTypes
{
    /// <summary>
    /// Gets the distinct registered identities consumed by an observer's invoker.
    /// </summary>
    /// <param name="handledEventTypes">The invoker's accepted event identities.</param>
    /// <returns>The consumed event identifiers.</returns>
    internal static IEnumerable<EventTypeId> GetIds(IEnumerable<EventType> handledEventTypes) =>
        handledEventTypes.Select(type => type.Id).Distinct().ToArray();

    /// <summary>
    /// Resolves an observer's source store, honoring its explicit store override.
    /// </summary>
    /// <param name="observerType">The observer type.</param>
    /// <param name="eventTypes">The event registry.</param>
    /// <param name="currentStore">The current client's store.</param>
    /// <param name="handledEventTypes">The owning invoker's accepted event identities.</param>
    /// <returns>The single source store, or null when there is none or several.</returns>
    internal static string? GetSourceStore(Type observerType, IEventTypes eventTypes, EventStoreName currentStore, IEnumerable<EventType> handledEventTypes)
    {
        if (observerType.GetCustomAttribute<EventStoreAttribute>() is { } declaredStore)
        {
            return declaredStore.EventStore;
        }

        var stores = GetStores(handledEventTypes, eventTypes, currentStore);
        return stores.Length == 1 ? stores[0] : null;
    }

    /// <summary>
    /// Infers the event sequence from the owning invoker's accepted event identities.
    /// </summary>
    /// <param name="observerType">The observer type.</param>
    /// <param name="eventTypes">The event registry.</param>
    /// <param name="currentStore">The current client's store.</param>
    /// <param name="handledEventTypes">The owning invoker's accepted event identities.</param>
    /// <returns>The inferred event sequence.</returns>
    /// <exception cref="MultipleEventStoresDefined">Handlers declare multiple source stores.</exception>
    internal static EventSequenceId InferSequence(Type observerType, IEventTypes eventTypes, EventStoreName currentStore, IEnumerable<EventType> handledEventTypes)
    {
        if (CanUseSequenceCache(eventTypes, currentStore))
        {
            return ObserverSequenceCache.Infer(observerType, (EventTypes)eventTypes, currentStore, handledEventTypes);
        }

        var stores = GetStores(handledEventTypes, eventTypes, currentStore);
        if (stores.Length > 1)
        {
            throw new MultipleEventStoresDefined(observerType, stores);
        }

        return stores.Length == 1 && stores[0] != currentStore.Value
            ? new EventSequenceId($"{EventSequenceId.InboxPrefix}{stores[0]}")
            : EventSequenceId.Log;
    }

    static bool CanUseSequenceCache(IEventTypes eventTypes, EventStoreName? currentStore) =>
        eventTypes.GetType() == typeof(EventTypes) && currentStore is not null;

    static string[] GetStores(IEnumerable<EventType> handledEventTypes, IEventTypes eventTypes, EventStoreName currentStore)
    {
        var handled = handledEventTypes.ToHashSet();
        return eventTypes.AllClrTypes
            .Where(type => handled.Contains(eventTypes.GetEventTypeFor(type)))
            .Select(type => eventTypes.GetDescriptorFor(type, currentStore).EventStore)
            .OfType<string>()
            .Distinct()
            .ToArray();
    }
}
