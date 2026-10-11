// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Resolves classic observer handler declarations against one client's event registry.
/// </summary>
internal static class ObserverEventTypes
{
    /// <summary>
    /// Gets the distinct registered identities consumed by an observer.
    /// </summary>
    /// <param name="observerType">The observer type.</param>
    /// <param name="eventTypes">The event registry.</param>
    /// <returns>The consumed event identifiers.</returns>
    internal static IEnumerable<EventTypeId> GetIds(Type observerType, IEventTypes eventTypes) =>
        GetHandlerTypes(observerType, eventTypes).Select(type => eventTypes.GetEventTypeFor(type).Id).Distinct().ToArray();

    /// <summary>
    /// Resolves an observer's source store, honoring its explicit store override.
    /// </summary>
    /// <param name="observerType">The observer type.</param>
    /// <param name="eventTypes">The event registry.</param>
    /// <param name="currentStore">The current client's store.</param>
    /// <returns>The single source store, or null when there is none or several.</returns>
    internal static string? GetSourceStore(Type observerType, IEventTypes eventTypes, EventStoreName currentStore)
    {
        if (observerType.GetCustomAttribute<EventStoreAttribute>() is { } declaredStore)
        {
            return declaredStore.EventStore;
        }

        var stores = GetStores(observerType, eventTypes, currentStore);
        return stores.Length == 1 ? stores[0] : null;
    }

    /// <summary>
    /// Infers the event sequence from registered handler metadata.
    /// </summary>
    /// <param name="observerType">The observer type.</param>
    /// <param name="eventTypes">The event registry.</param>
    /// <param name="currentStore">The current client's store.</param>
    /// <returns>The inferred event sequence.</returns>
    /// <exception cref="MultipleEventStoresDefined">Handlers declare multiple source stores.</exception>
    internal static EventSequenceId InferSequence(Type observerType, IEventTypes eventTypes, EventStoreName currentStore)
    {
        var stores = GetStores(observerType, eventTypes, currentStore);
        if (stores.Length > 1)
        {
            throw new MultipleEventStoresDefined(observerType, stores);
        }

        return stores.Length == 1 && stores[0] != currentStore.Value
            ? new EventSequenceId($"{EventSequenceId.InboxPrefix}{stores[0]}")
            : EventSequenceId.Log;
    }

    static string[] GetStores(Type observerType, IEventTypes eventTypes, EventStoreName currentStore) =>
        GetHandlerTypes(observerType, eventTypes)
            .Select(type => eventTypes.GetDescriptorFor(type, currentStore).EventStore)
            .OfType<string>()
            .Distinct()
            .ToArray();

    static IEnumerable<Type> GetHandlerTypes(Type observerType, IEventTypes eventTypes) =>
        observerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.GetParameters().FirstOrDefault()?.ParameterType)
            .OfType<Type>()
            .Where(type => IsCurrentEventType(type, eventTypes))
            .Distinct();

    static bool IsCurrentEventType(Type type, IEventTypes eventTypes)
    {
        if (Attribute.IsDefined(type, typeof(EventTypeAttribute)))
        {
            return true;
        }

        if (type == typeof(object) || Attribute.IsDefined(type, typeof(EventTypeGenerationForAttribute)))
        {
            return false;
        }

        return eventTypes.HasFor(type);
    }
}
