// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Serialization;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Resolves declared index paths through CLR properties and the serialization naming policy.
/// </summary>
internal static class ReadModelIndexPath
{
    /// <summary>
    /// Resolves a declared path and whether it passes through a collection.
    /// </summary>
    /// <param name="type">The declaring type.</param>
    /// <param name="path">The declared property path.</param>
    /// <param name="namingPolicy">The serialization naming policy.</param>
    /// <returns>The serialized path and collection classification.</returns>
    /// <exception cref="InvalidIndexDeclaration">The path contains an unknown property or an invalid collection marker.</exception>
    internal static (string Path, IReadOnlyList<string> ArrayPaths) Resolve(Type type, string path, INamingPolicy namingPolicy)
    {
        var declaringType = type;
        var segments = (path ?? string.Empty).Split('.');
        var arrayPaths = new List<string>();
        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            var name = segment.EndsWith("[]", StringComparison.Ordinal) ? segment[..^2] : segment;
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property is null || property.GetIndexParameters().Length > 0)
            {
                throw new InvalidIndexDeclaration(declaringType, $"property path '{path}' contains unknown property '{segment}'");
            }
            var elementType = ElementType(property.PropertyType);
            if (segment.EndsWith("[]", StringComparison.Ordinal) && elementType is null)
            {
                throw new InvalidIndexDeclaration(declaringType, $"property '{name}' in path '{path}' is not a collection");
            }
            type = elementType ?? Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            segments[index] = namingPolicy.GetPropertyName(property.Name);
            if (elementType is not null) arrayPaths.Add(string.Join('.', segments.Take(index + 1)));
        }
        return (string.Join('.', segments), arrayPaths);
    }

    /// <summary>
    /// Gets the element type for a collection, excluding strings.
    /// </summary>
    /// <param name="type">The property type.</param>
    /// <returns>The collection element type, or null.</returns>
    internal static Type? ElementType(Type type)
    {
        if (type == typeof(string)) return null;
        if (type.IsArray) return type.GetElementType();
        var enumerable = type.GetInterfaces().Prepend(type)
            .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }
}
