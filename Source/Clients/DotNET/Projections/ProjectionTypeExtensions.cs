// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.Validators;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Extension methods for working with projection types.
/// </summary>
public static class ProjectionTypeExtensions
{
    /// <summary>
    /// Get the reducer id for a type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>The <see cref="ProjectionId"/> for the type.</returns>
    public static ProjectionId GetProjectionId(this Type type)
    {
        TypeMustImplementProjection.ThrowIfTypeDoesNotImplementProjection(type);
        var projectionAttribute = type.GetCustomAttribute<ProjectionAttribute>();
        var id = projectionAttribute?.Id.Value ?? string.Empty;
        return id switch
        {
            "" => new ProjectionId(type.FullName ?? $"{type.Namespace}.{type.Name}"),
            _ => new ProjectionId(id)
        };
    }

    /// <summary>
    /// Get the event sequence id for a projection type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>The <see cref="EventSequenceId"/> for the type.</returns>
    public static EventSequenceId GetEventSequenceId(this Type type)
    {
        TypeMustImplementProjection.ThrowIfTypeDoesNotImplementProjection(type);
        var projectionAttribute = type.GetCustomAttribute<ProjectionAttribute>();
        return projectionAttribute?.EventSequenceId.Value ?? EventSequenceId.Log;
    }

    /// <summary>
    /// Get the type of the read model for a reducer.
    /// </summary>
    /// <param name="type">Reducer type to get for.</param>
    /// <returns>Type of read model.</returns>
    public static Type GetReadModelType(this Type type)
    {
        TypeMustImplementProjection.ThrowIfTypeDoesNotImplementProjection(type);
        var interfaces = type.GetInterfaces();
        var reducerInterface = interfaces.Single(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IProjectionFor<>));
        return reducerInterface.GetGenericArguments()[0];
    }
}
