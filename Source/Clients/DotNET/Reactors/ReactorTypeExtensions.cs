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
    /// <returns>The <see cref="EventSequenceId"/> for the type.</returns>
    /// <exception cref="MultipleEventStoresDefined">Thrown when the reactor handles event types from multiple event stores.</exception>
    public static EventSequenceId GetEventSequenceId(this Type type)
    {
        TypeMustImplementReactor.ThrowIfTypeDoesNotImplementReactor(type);
        var reactorAttribute = type.GetCustomAttribute<ReactorAttribute>();

        if (reactorAttribute?.EventSequenceId is not null)
        {
            return reactorAttribute.EventSequenceId;
        }

        return InferEventSequenceIdFromHandlerMethods(type);
    }

    /// <summary>
    /// Find all Reactors.
    /// </summary>
    /// <param name="types">Collection of types.</param>
    /// <returns>Collection of types that are Reactors.</returns>
    public static IEnumerable<Type> AllReactors(this IEnumerable<Type> types) => types.Where(_ => _.HasAttribute<ReactorAttribute>()).ToArray();

    static EventSequenceId InferEventSequenceIdFromHandlerMethods(Type reactorType)
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
            return new EventSequenceId($"{EventSequenceId.InboxPrefix}{eventStores[0]}");
        }

        return EventSequenceId.Log;
    }
}
