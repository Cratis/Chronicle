// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Finds <see cref="SubjectFromAttribute"/> declarations sitting below a read model's own properties, where
/// they cannot be honored.
/// </summary>
/// <remarks>
/// The scan runs once per read model type, together with the rest of its
/// <see cref="ReadModelReleasePlan"/>. It prunes everything a declaration cannot be written on — primitives,
/// enums, and the whole of the base class library — so what it walks is the application's own value objects
/// and child models.
/// </remarks>
internal static class ReadModelReleaseDeclarationScanner
{
    const int MaxDepth = 16;

    /// <summary>
    /// Throw when any type reachable from the read model's properties carries a declaration.
    /// </summary>
    /// <param name="readModelType">The read model <see cref="Type"/> being planned.</param>
    /// <param name="properties">The read model's own properties, whose declarations are legitimate.</param>
    /// <exception cref="ReleaseUnderNotSupportedBelowReadModel">A nested type carries a declaration.</exception>
    public static void ThrowIfDeclaredBelowReadModel(Type readModelType, PropertyInfo[] properties)
    {
        var visited = new HashSet<Type> { readModelType };
        foreach (var property in properties)
        {
            Scan(readModelType, property.PropertyType, visited, 1);
        }
    }

    static void Scan(Type readModelType, Type type, HashSet<Type> visited, int depth)
    {
        if (Candidate(type) is not { } candidate || depth > MaxDepth || !visited.Add(candidate))
        {
            return;
        }

        foreach (var property in candidate.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (IsDeclared(property))
            {
                throw new ReleaseUnderNotSupportedBelowReadModel(readModelType, candidate, property.Name);
            }

            Scan(readModelType, property.PropertyType, visited, depth + 1);
        }
    }

    static bool IsDeclared(PropertyInfo property) =>
        property.IsDefined(typeof(SubjectFromAttribute), inherit: false) ||
        IsDeclaredOnConstructorParameter(property);

    static bool IsDeclaredOnConstructorParameter(PropertyInfo property)
    {
        var constructor = property.DeclaringType?.GetConstructors().MaxBy(candidate => candidate.GetParameters().Length);
        return constructor?
            .GetParameters()
            .Any(parameter =>
                string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase) &&
                parameter.IsDefined(typeof(SubjectFromAttribute), inherit: false)) ?? false;
    }

    static Type? Candidate(Type type)
    {
        var unwrapped = Nullable.GetUnderlyingType(type) ?? type;

        if (unwrapped.IsArray)
        {
            return Candidate(unwrapped.GetElementType()!);
        }

        if (unwrapped != typeof(string) && typeof(IEnumerable).IsAssignableFrom(unwrapped) && unwrapped.IsGenericType)
        {
            return Candidate(unwrapped.GetGenericArguments()[^1]);
        }

        var prunable = unwrapped.IsPrimitive ||
            unwrapped.IsEnum ||
            unwrapped.IsPointer ||
            unwrapped.IsGenericParameter ||
            unwrapped == typeof(string) ||
            unwrapped == typeof(object) ||
            (unwrapped.Namespace?.StartsWith("System", StringComparison.Ordinal) ?? false);

        return prunable ? null : unwrapped;
    }
}
