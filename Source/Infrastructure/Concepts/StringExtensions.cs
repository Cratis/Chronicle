// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts;

/// <summary>
/// Provides extensions related to strings and conecpts.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Convert a string into the desired type.
    /// </summary>
    /// <param name="input">the string to parse.</param>
    /// <param name="type">the desired type.</param>
    /// <returns>value as the desired type.</returns>
    public static object ParseTo(this string input, Type type)
    {
        if (type == typeof(Guid))
        {
            if (Guid.TryParse(input, out var result)) return result;
            return Guid.Empty;
        }

        if (type.IsConcept())
        {
            var primitiveType = type.GetConceptValueType();
            var primitive = ParseTo(input, primitiveType);
            return ConceptFactory.CreateConceptInstance(type, primitive);
        }

        return Convert.ChangeType(input, type, null);
    }
}
