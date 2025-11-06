// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Extension methods for model-bound projections.
/// </summary>
public static class ModelBoundProjectionsExtensions
{
    /// <summary>
    /// Determines whether a type has model-bound projection attributes.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type has model-bound projection attributes; otherwise, false.</returns>
    public static bool HasModelBoundProjectionAttributes(this Type type)
    {
        if (type.GetProperties().Any(p => p.GetCustomAttribute<KeyAttribute>() is not null))
        {
            return true;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.Any(property => property.GetCustomAttributes()
                                                  .Any(attr => attr is IProjectionAnnotation));
    }
}
