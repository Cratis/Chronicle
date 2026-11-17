// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Concepts;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Compares the identifying value of a child with a key.
/// </summary>
internal static class ChildKeyComparison
{
    /// <summary>
    /// Compare a child's identifying value with a key by value, looking through concepts.
    /// </summary>
    /// <remarks>
    /// Values of the same type compare with their own equality and composite keys compare property by property. Values of
    /// different types - such as a Guid against its string form - compare by their invariant text, the only common ground
    /// they have.
    /// </remarks>
    /// <param name="value">The value identifying the child.</param>
    /// <param name="key">The key to compare with.</param>
    /// <returns>True if they identify the same child, false if not.</returns>
    internal static bool IsSameKey(object? value, object? key)
    {
        value = UnwrapConcept(value);
        key = UnwrapConcept(key);
        if (value is null || key is null)
        {
            return value is null && key is null;
        }

        if (value is IDictionary<string, object?> valueProperties && key is IDictionary<string, object?> keyProperties)
        {
            return HaveSameProperties(valueProperties, keyProperties);
        }

        return value.GetType() == key.GetType() ? value.Equals(key) : HaveSameText(value, key);
    }

    static bool HaveSameProperties(IDictionary<string, object?> valueProperties, IDictionary<string, object?> keyProperties) =>
        valueProperties.Count == keyProperties.Count &&
        valueProperties.All(_ => keyProperties.TryGetValue(_.Key, out var keyValue) && IsSameKey(_.Value, keyValue));

    static bool HaveSameText(object value, object key) =>
        string.Equals(
            Convert.ToString(value, CultureInfo.InvariantCulture),
            Convert.ToString(key, CultureInfo.InvariantCulture),
            StringComparison.Ordinal);

    static object? UnwrapConcept(object? value) => value?.IsConcept() == true ? value.GetConceptValue() : value;
}
