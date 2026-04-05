// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.Validators;
using Cratis.Reflection;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Extension methods for working with Reactors and type discovery.
/// </summary>
public static class ReactorTypeExtensions
{
    /// <summary>
    /// Get the Reactor id for a Reactor type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>The <see cref="ReactorId"/> for the type.</returns>
    public static ReactorId GetReactorId(this Type type)
    {
        TypeMustImplementReactor.ThrowIfTypeDoesNotImplementReactor(type);
        var reactorAttribute = type.GetCustomAttribute<ReactorAttribute>();
        var id = reactorAttribute?.Id.Value ?? string.Empty;
        return id switch
        {
            "" => new ReactorId(type.FullName ?? $"{type.Namespace}.{type.Name}"),
            _ => new ReactorId(id)
        };
    }

    /// <summary>
    /// Get the event sequence id for a Reactor type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <param name="currentEventStoreName">
    /// The name of the event store the reactor is registered in.
    /// When provided, event types that belong to the same store will resolve to the event log
    /// rather than an inbox sequence.
    /// </param>
    /// <returns>The <see cref="EventSequenceId"/> for the type.</returns>
    /// <exception cref="MultipleEventStoresDefined">Thrown when the reactor handles event types from multiple event stores.</exception>
    public static EventSequenceId GetEventSequenceId(this Type type, string? currentEventStoreName = null)
    {
        TypeMustImplementReactor.ThrowIfTypeDoesNotImplementReactor(type);

        // [EventSequence] / [EventLog] on the class takes highest priority
        var eventSequenceAttr = type.GetCustomAttribute<EventSequenceAttribute>();
        if (eventSequenceAttr is not null)
        {
            return eventSequenceAttr.Sequence;
        }

        // [Reactor(eventSequence: "...")] is the second priority
        var reactorAttribute = type.GetCustomAttribute<ReactorAttribute>();
        if (reactorAttribute?.EventSequenceId is not null)
        {
            return reactorAttribute.EventSequenceId;
        }

        return InferEventSequenceIdFromHandlerMethods(type, currentEventStoreName);
    }

    /// <summary>
    /// Get whether a Reactor type has an explicit event sequence set.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns><see langword="true"/> if an explicit event sequence is configured; otherwise <see langword="false"/>.</returns>
    public static bool HasExplicitEventSequence(this Type type)
    {
        if (Attribute.IsDefined(type, typeof(EventSequenceAttribute)))
        {
            return true;
        }

        var reactorAttribute = type.GetCustomAttribute<ReactorAttribute>();
        return reactorAttribute?.EventSequenceId is not null;
    }

    /// <summary>
    /// Get all event types used in the handler method signatures of a reactor type.
    /// </summary>
    /// <remarks>
    /// A handler method is any non-special public or non-public instance method whose first parameter type
    /// carries the <see cref="EventTypeAttribute"/>. Duplicates are removed so each event type appears at most once.
    /// </remarks>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>All event <see cref="Type">types</see> found as first parameters in handler methods.</returns>
    public static IEnumerable<Type> GetHandlerEventTypes(this Type type) =>
        type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.GetParameters().FirstOrDefault()?.ParameterType)
            .Where(t => t is not null && Attribute.IsDefined(t, typeof(EventTypeAttribute)))
            .Select(t => t!)
            .Distinct()
            .ToList();

    /// <summary>
    /// Find all Reactors.
    /// </summary>
    /// <param name="types">Collection of types.</param>
    /// <returns>Collection of types that are Reactors.</returns>
    public static IEnumerable<Type> AllReactors(this IEnumerable<Type> types) => types.Where(_ => _.HasAttribute<ReactorAttribute>()).ToArray();

    static EventSequenceId InferEventSequenceIdFromHandlerMethods(Type reactorType, string? currentEventStoreName)
    {
        var eventParameterTypes = reactorType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.GetParameters().FirstOrDefault()?.ParameterType)
            .Where(t => t is not null && Attribute.IsDefined(t, typeof(EventTypeAttribute)))
            .Distinct()
            .ToList();

        var eventStores = eventParameterTypes
            .Select(t => t!.GetEventStoreName())
            .Where(name => name is not null)
            .Select(name => name!)
            .Distinct()
            .ToList();

        if (eventStores.Count > 1)
        {
            throw new MultipleEventStoresDefined(reactorType, eventStores);
        }

        if (eventStores.Count == 1)
        {
            // If the event types belong to the same store as the one we're in, use event-log instead of inbox
            if (currentEventStoreName is not null && eventStores[0] == currentEventStoreName)
            {
                return EventSequenceId.Log;
            }

            return new EventSequenceId($"{EventSequenceId.InboxPrefix}{eventStores[0]}");
        }

        return EventSequenceId.Log;
    }
}
