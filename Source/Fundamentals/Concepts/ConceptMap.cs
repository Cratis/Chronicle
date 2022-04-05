// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Aksio.Cratis.Concepts;

/// <summary>
/// Maps a concept type to the underlying primitive type.
/// </summary>
public static class ConceptMap
{
    static readonly ConcurrentDictionary<Type, Type> _cache = new();

    /// <summary>
    /// Get the type of the value in a <see cref="ConceptAs{T}"/>.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get value type from.</param>
    /// <returns>The type of the <see cref="ConceptAs{T}"/> value.</returns>
    public static Type GetConceptValueType(Type type)
    {
        return _cache.GetOrAdd(type, GetPrimitiveType);
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
