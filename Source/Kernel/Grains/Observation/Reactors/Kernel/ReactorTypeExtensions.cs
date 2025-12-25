// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

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
    public static EventSequenceId GetEventSequenceId(this Type type)
    {
        TypeMustImplementReactor.ThrowIfTypeDoesNotImplementReactor(type);
        var reactorAttribute = type.GetCustomAttribute<ReactorAttribute>();
        return reactorAttribute?.EventSequenceId.Value ?? EventSequenceId.Log;
    }
}
