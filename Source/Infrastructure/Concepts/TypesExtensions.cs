// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Cratis.Types;

namespace Cratis.Concepts;

/// <summary>
/// Provides extensions related to working with <see cref="ITypes"/>.
/// </summary>
public static class TypesExtensions
{
    /// <summary>
    /// Register type converters for all <see cref="ConceptAs{T}"/> types.
    /// </summary>
    /// <param name="types"><see cref="ITypes"/> to extend.</param>
    /// <returns><see cref="ITypes"/> for continuation.</returns>
    public static ITypes RegisterTypeConvertersForConcepts(this ITypes types)
    {
        foreach (var conceptType in types.FindMultiple(typeof(ConceptAs<>)))
        {
            var typeConverterType = typeof(ConceptAsTypeConverter<,>).MakeGenericType(conceptType, conceptType.GetConceptValueType());
            TypeDescriptor.AddAttributes(conceptType, new TypeConverterAttribute(typeConverterType));
        }

        return types;
    }
}
