// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Reflection;

namespace Cratis.Concepts;

/// <summary>
/// Provides extensions related to <see cref="Type">types</see> and others related to <see cref="ConceptAs{T}"/>.
/// </summary>
public static class ConceptExtensions
{
    /// <summary>
    /// Check if a type is a concept or not.
    /// </summary>
    /// <param name="objectType"><see cref="Type"/> to check.</param>
    /// <returns>True if type is a concept, false if not.</returns>
    public static bool IsConcept(this Type objectType)
    {
        return objectType.IsDerivedFromOpenGeneric(typeof(ConceptAs<>));
    }

    /// <summary>
    /// Check if an object is an instance of a concept or not.
    /// </summary>
    /// <param name="instance">instance to check.</param>
    /// <returns>True if object is a concept, false if not.</returns>
    public static bool IsConcept(this object instance)
    {
        return IsConcept(instance.GetType());
    }

    /// <summary>
    /// Get the type of the value inside a <see cref="ConceptAs{T}"/>.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get value type from.</param>
    /// <returns>The type of the <see cref="ConceptAs{T}"/> value.</returns>
    public static Type GetConceptValueType(this Type type)
    {
        return ConceptMap.GetConceptValueType(type);
    }

    /// <summary>
    /// Takes a Concept{T} value object as an object and returns the correct primitive value, also as an object.
    /// </summary>
    /// <param name="conceptObject">The concept as an object.</param>
    /// <returns>The value of the primitive type on which the concept is based.</returns>
    /// <exception cref="TypeIsNotAConcept">Thrown if type is not a concept.</exception>
    public static object GetConceptValue(this object conceptObject)
    {
        if (!IsConcept(conceptObject)) throw new TypeIsNotAConcept(conceptObject.GetType());

        return conceptObject.GetType()!.GetTypeInfo()!.GetProperty(nameof(ConceptAs<string>.Value))!.GetValue(conceptObject, null)!;
    }
}
