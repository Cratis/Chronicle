// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for working with observer types.
/// </summary>
public static class ObserverTypeExtensions
{
    /// <summary>
    /// Get the observer id for a type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get from.</param>
    /// <returns>The <see cref="ObserverId"/> for the type.</returns>
    public static ObserverId GetObserverId(this Type type)
    {
        var observerAttribute = type.GetCustomAttribute<ObserverAttribute>()!;
        return observerAttribute.Id.Value switch
        {
            "" => new ObserverId(type.FullName ?? $"{type.Namespace}.{type.Name}"),
            _ => new ObserverId(observerAttribute.Id)
        };
    }
}
