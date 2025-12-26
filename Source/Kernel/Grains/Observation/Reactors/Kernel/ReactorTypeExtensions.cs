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

    /// <summary>
    /// Determine whether a Reactor type is a system reactor.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if the type is a system reactor, false otherwise.</returns>
    public static bool IsSystemReactor(this Type type)
    {
        TypeMustImplementReactor.ThrowIfTypeDoesNotImplementReactor(type);
        var reactorAttribute = type.GetCustomAttribute<ReactorAttribute>();
        return reactorAttribute?.IsSystem ?? true;
    }

    /// <summary>
    /// Determine whether a Reactor type is limited to the default namespace only.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if the type is limited to the default namespace only, false otherwise.</returns>
    public static bool IsDefaultNamespaceOnly(this Type type)
    {
        TypeMustImplementReactor.ThrowIfTypeDoesNotImplementReactor(type);
        var reactorAttribute = type.GetCustomAttribute<ReactorAttribute>();
        return reactorAttribute?.DefaultNamespaceOnly ?? true;
    }
}
