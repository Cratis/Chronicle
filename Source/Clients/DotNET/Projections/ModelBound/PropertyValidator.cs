// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Properties;

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
    public static string ValidatePropertyExists<T>(string propertyName) => ValidatePropertyExists(typeof(T), propertyName);

    /// <summary>
    /// Validates that a property name exists on a given type.
    /// </summary>
    /// <param name="type">The type to validate against.</param>
    /// <param name="propertyName">The property name to validate.</param>
    /// <exception cref="InvalidPropertyForType">Thrown when the property does not exist on the type.</exception>
    /// <returns>The validated property name.</returns>
    public static string ValidatePropertyExists(Type type, string propertyName)
    {
        var currentType = type;
        foreach (var segment in propertyName.Split('.'))
        {
            var property = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                ?? throw new InvalidPropertyForType(type, propertyName);
            currentType = property.PropertyType;
        }

        return propertyName;
    }

    /// <summary>
    /// Validates a context path, allowing a compatible registered terminal derived function.
    /// </summary>
    /// <typeparam name="T">The context type to validate against.</typeparam>
    /// <param name="propertyName">The context property path.</param>
    /// <returns>The canonical CLR path, with derived functions rendered as argument-free calls.</returns>
    /// <exception cref="InvalidPropertyForType">The path or its derived receiver is invalid.</exception>
    public static string ValidateContextPropertyPath<T>(string propertyName) => ValidateContextPropertyPath(typeof(T), propertyName);

    /// <summary>
    /// Validates a context path, allowing a compatible registered terminal derived function.
    /// Real CLR properties take precedence over bare derived function names.
    /// </summary>
    /// <param name="type">The context type to validate against.</param>
    /// <param name="propertyName">The context property path.</param>
    /// <returns>The canonical CLR path, with derived functions rendered as argument-free calls.</returns>
    /// <exception cref="InvalidPropertyForType">The path or its derived receiver is invalid.</exception>
    public static string ValidateContextPropertyPath(Type type, string propertyName)
    {
        var currentType = type;
        var segments = propertyName.Split('.');
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            var property = currentType.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is not null)
            {
                segments[index] = property.Name;
                currentType = property.PropertyType;
                continue;
            }

            var functionName = segment.EndsWith("()", StringComparison.Ordinal) ? segment[..^2] : segment;
            var receiverType = Nullable.GetUnderlyingType(currentType) ?? currentType;
            if (index == 0 || index != segments.Length - 1 ||
                !DerivedPropertyFunctions.TryGet(functionName, out var function) ||
                !function.SupportedReceiverTypes.Contains(receiverType))
            {
                throw new InvalidPropertyForType(type, propertyName);
            }

            segments[index] = $"{function.Name}()";
        }

        return string.Join('.', segments);
    }
}
