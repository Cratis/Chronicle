// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;

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
        // Event-sequence attributes also select where reactors and reducers observe events. They are not
        // evidence that those types (or an event type) are read models with a projection to register.
        if (!type.IsClass || typeof(IReactor).IsAssignableFrom(type) ||
            typeof(IReducer).IsAssignableFrom(type) || typeof(IReadModelReactor).IsAssignableFrom(type) ||
            type.IsDefined(typeof(EventTypeAttribute)) || type.IsDefined(typeof(EventTypeGenerationForAttribute)))
        {
            return false;
        }

        try
        {
            if (type.GetCustomAttributes().Any(IsModelBoundProjectionAttribute))
            {
                return true;
            }

            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

            if (primaryConstructor is not null)
            {
                var parameters = primaryConstructor.GetParameters();
                if (parameters.Any(param => param.GetCustomAttributes()
                                                    .Any(IsModelBoundProjectionAttribute)))
                {
                    return true;
                }
            }

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties.Any(property => property.GetCustomAttributes()
                                                      .Any(IsModelBoundProjectionAttribute));
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or TypeLoadException or ReflectionTypeLoadException)
        {
            return false;
        }
    }

    static bool IsModelBoundProjectionAttribute(object attribute) =>
        attribute is EventSequenceAttribute ||
        attribute is IProjectionAnnotation and not PassiveAttribute;
}
