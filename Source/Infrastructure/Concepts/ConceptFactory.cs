// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;

#nullable disable

namespace Cratis.Concepts;

/// <summary>
/// Factory to create an instance of a <see cref="ConceptAs{T}"/> from the Type and Underlying value.
/// </summary>
public static class ConceptFactory
{
    /// <summary>
    /// Creates an instance of a <see cref="ConceptAs{T}"/> given the type and underlying value.
    /// </summary>
    /// <param name="type">Type of the ConceptAs to create.</param>
    /// <param name="value">Value to give to this instance.</param>
    /// <returns>An instance of a ConceptAs with the specified value.</returns>
    public static object CreateConceptInstance(Type type, object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var genericArgumentType = GetPrimitiveTypeConceptIsBasedOn(type);
        value = TypeConversion.Convert(genericArgumentType, value);
        var instance = Activator.CreateInstance(type, value);
        ConceptMap.GetValuePropertyInfo(type).SetValue(instance, value, null);
        return instance;
    }

    static Type GetPrimitiveTypeConceptIsBasedOn(Type conceptType)
    {
        return ConceptMap.GetConceptValueType(conceptType);
    }
}
