// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Selects an exact string leaf without depending on property casing or scalar formatting.
/// </summary>
internal static class CompositeKeyCandidate
{
    /// <summary>
    /// Finds a string that every structurally matching identity must contain.
    /// </summary>
    /// <param name="value">The composite identity.</param>
    /// <returns>A string leaf, or null when narrowing cannot be proven safe.</returns>
    internal static string? StringLeaf(object? value)
    {
        try
        {
            // Normalization validates the entire identity and turns canonical Guid strings into Guids.
            // Guid, numeric, boolean and null leaves deliberately do not constrain the SQL query.
            return FindString(StructuralKeyNormalizer.NormalizeRoot(value));
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidOperationException or ArgumentException or TargetInvocationException)
        {
            return null;
        }
    }

    static string? FindString(object? value) => value switch
    {
        string text => text,
        Dictionary<string, object?> properties => FindStringIn(properties.Values),
        List<object?> values => FindStringIn(values),
        _ => null
    };

    static string? FindStringIn(IEnumerable<object?> values) => values.Select(FindString).FirstOrDefault(value => value is not null);
}
