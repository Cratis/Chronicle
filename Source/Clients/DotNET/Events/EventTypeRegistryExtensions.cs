// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events;

/// <summary>
/// Internal metadata access shared by the client registry and existing custom registries.
/// </summary>
internal static class EventTypeRegistryExtensions
{
    /// <summary>
    /// Resolves metadata through the registry, retaining declared metadata for custom implementations.
    /// </summary>
    /// <param name="eventTypes">The registry.</param>
    /// <param name="type">The event CLR type.</param>
    /// <param name="eventStore">The current event store.</param>
    /// <returns>The registered descriptor.</returns>
    internal static EventTypeDescriptor GetDescriptorFor(this IEventTypes eventTypes, Type type, EventStoreName eventStore) =>
        eventTypes is EventTypes registry
            ? registry.GetDescriptorFor(type)
            : EventTypeDescriptor.FromRegisteredType(type, eventTypes.GetEventTypeFor(type), eventStore);

    /// <summary>
    /// Identifies registered event targets while retaining rejection of unregistered declared targets.
    /// </summary>
    /// <param name="eventTypes">The registry.</param>
    /// <param name="type">The target CLR type.</param>
    /// <returns>Whether the target must use the event sequence sink.</returns>
    internal static bool IsEventTarget(this IEventTypes eventTypes, Type type) =>
        Sinks.EventTargetExtensions.IsEventTypeTarget(type) || eventTypes.HasFor(type);
}
