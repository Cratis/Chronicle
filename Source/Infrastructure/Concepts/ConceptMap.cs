// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Cratis.Concepts;

/// <summary>
/// Maps a concept type to the underlying primitive type.
/// </summary>
public static class ConceptMap
{
    static readonly ConcurrentDictionary<Type, Type> _primitiveTypeCache = new();
    static readonly ConcurrentDictionary<Type, PropertyInfo> _valuePropertyCache = new();

    /// <summary>
    /// Get the type of the value in a <see cref="ConceptAs{T}"/>.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get value type from.</param>
    /// <returns>The type of the <see cref="ConceptAs{T}"/> value.</returns>
    public static Type GetConceptValueType(Type type)
    {
        if (_primitiveTypeCache.ContainsKey(type)) return _primitiveTypeCache[type];

        var primitiveType = GetPrimitiveType(type);
        _primitiveTypeCache.TryAdd(type, primitiveType);
        return primitiveType;
    }

    /// <summary>
    /// Get the <see cref="PropertyInfo"/> for the value property in a <see cref="ConceptAs{T}"/>.
    /// </summary>
    /// <param name="type">Type to get for.</param>
    /// <returns><see cref="PropertyInfo"/> for the concept type.</returns>
    public static PropertyInfo GetValuePropertyInfo(Type type)
    {
        if (_valuePropertyCache.ContainsKey(type)) return _valuePropertyCache[type];

        var valueProperty = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        _valuePropertyCache.TryAdd(type, valueProperty!);
        return valueProperty!;
    }

    static Type GetPrimitiveType(Type type)
    {
        var conceptType = type;
        for (; ; )
        {
            if (conceptType == typeof(ConceptAs<>) || conceptType.BaseType == null) break;

            if (conceptType.BaseType.IsGenericType && conceptType.BaseType.GetGenericTypeDefinition() == typeof(ConceptAs<>))
            {
                return conceptType.BaseType.GetGenericArguments()[0];
            }

            if (conceptType == typeof(object)) break;

            conceptType = conceptType.GetTypeInfo().BaseType!;
        }

        return typeof(void);
    }
}
