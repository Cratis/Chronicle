// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

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
        if (type.GetCustomAttributes().Any(attr => attr is IProjectionAnnotation))
        {
            return true;
        }

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

        if (primaryConstructor is not null)
        {
            var parameters = primaryConstructor.GetParameters();
            if (parameters.Any(param => param.GetCustomAttributes()
                                                .Any(attr => attr is IProjectionAnnotation)))
            {
                return true;
            }
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.Any(property => property.GetCustomAttributes()
                                                  .Any(attr => attr is IProjectionAnnotation));
    }
}
