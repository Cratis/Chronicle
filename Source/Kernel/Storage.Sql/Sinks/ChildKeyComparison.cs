// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Distinguishes structural child identities from scalar identities that retain loose string matching.
/// </summary>
internal static class ChildKeyComparison
{
    /// <summary>
    /// Checks whether either identity must be compared by its materialized value rather than its string form.
    /// </summary>
    /// <param name="stored">The stored identity.</param>
    /// <param name="lookup">The lookup identity.</param>
    /// <returns>Whether structural equality is required.</returns>
    internal static bool RequiresStructuralComparison(object? stored, object? lookup) => StructuralKeyEquality.IsStructured(stored) || StructuralKeyEquality.IsStructured(lookup);

    /// <summary>
    /// Compares structural identities without allowing scalar conversion to equate an object with its type name.
    /// </summary>
    /// <param name="stored">The stored identity.</param>
    /// <param name="lookup">The lookup identity.</param>
    /// <returns>Whether the materialized identities are equal.</returns>
    internal static bool AreEqual(object? stored, object? lookup) =>
        KeyValueEquality.AreEqual(JsonColumnDocuments.UnwrapConceptValue(stored), JsonColumnDocuments.UnwrapConceptValue(lookup));

    /// <summary>
    /// Keeps composite indexer identities intact while preserving scalar string normalization.
    /// </summary>
    /// <param name="value">The indexer identity.</param>
    /// <returns>The identity used to navigate a deserialized JSON collection.</returns>
    internal static object? NormalizeIndexerValue(object? value)
    {
        var unwrapped = JsonColumnDocuments.UnwrapConceptValue(value);
        return StructuralKeyEquality.IsStructured(unwrapped) ? unwrapped : unwrapped?.ToString();
    }

    /// <summary>
    /// Checks whether scalar SQL candidate predicates cannot safely narrow a JSON identity search.
    /// </summary>
    /// <param name="value">The unwrapped lookup identity.</param>
    /// <returns>Whether the JSON documents must reach the in-memory identity comparison without scalar filtering.</returns>
    internal static bool RequiresJsonScan([NotNullWhen(false)] object? value) =>
        value is null || StructuralKeyEquality.IsStructured(value) || value.ToString()?.StartsWith("System.", StringComparison.Ordinal) == true;
}
