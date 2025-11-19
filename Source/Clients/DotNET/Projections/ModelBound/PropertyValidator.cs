// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Provides validation for property names against types.
/// </summary>
public static class PropertyValidator
{
    /// <summary>
    /// Validates that a property name exists on a given type.
    /// </summary>
    /// <typeparam name="T">The type to validate against.</typeparam>
    /// <param name="propertyName">The property name to validate.</param>
    /// <exception cref="InvalidPropertyForType">Thrown when the property does not exist on the type.</exception>
    /// <returns>The validated property name.</returns>
    public static string ValidatePropertyExists<T>(string propertyName)
    {
        var type = typeof(T);
        if (type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is null)
        {
            throw new InvalidPropertyForType(type, propertyName);
        }

        return propertyName;
    }

    /// <summary>
    /// Validates that a property name exists on a given type.
    /// </summary>
    /// <param name="type">The type to validate against.</param>
    /// <param name="propertyName">The property name to validate.</param>
    /// <exception cref="InvalidPropertyForType">Thrown when the property does not exist on the type.</exception>
    /// <returns>The validated property name.</returns>
    public static string ValidatePropertyExists(Type type, string propertyName)
    {
        if (type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is null)
        {
            throw new InvalidPropertyForType(type, propertyName);
        }

        return propertyName;
    }
}
