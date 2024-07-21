// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

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
        var projectionAttribute = type.GetCustomAttribute<ProjectionAttribute>();
        var id = projectionAttribute?.Id.Value ?? string.Empty;
        return id switch
        {
            "" => new ProjectionId(type.FullName ?? $"{type.Namespace}.{type.Name}"),
            _ => new ProjectionId(id)
        };
    }
}
