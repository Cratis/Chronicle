// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Identifies a child in a collection held in a JSON column by the name and value of its identifying property.
/// </summary>
internal sealed class ChildIdentifier
{
    ChildIdentifier(PropertyPath name, object? value)
    {
        Name = name;
        Value = value;
        AsString = value?.ToString();
    }

    /// <summary>
    /// Gets the <see cref="PropertyPath"/> of the identifying property.
    /// </summary>
    internal PropertyPath Name { get; }

    /// <summary>
    /// Gets the identifier value.
    /// </summary>
    internal object? Value { get; }

    /// <summary>
    /// Gets the identifier value in string form, converted once.
    /// </summary>
    internal string? AsString { get; }

    /// <summary>
    /// Create a <see cref="ChildIdentifier"/> from a name and a value.
    /// </summary>
    /// <param name="name">The <see cref="PropertyPath"/> of the identifying property.</param>
    /// <param name="value">The identifier value.</param>
    /// <returns>The <see cref="ChildIdentifier"/>.</returns>
    internal static ChildIdentifier For(PropertyPath name, object? value) => new(name, value);

    /// <summary>
    /// Create a <see cref="ChildIdentifier"/> from an <see cref="ArrayIndexer"/>, unwrapping a concept identifier.
    /// </summary>
    /// <param name="indexer">The <see cref="ArrayIndexer"/>.</param>
    /// <returns>The <see cref="ChildIdentifier"/>.</returns>
    internal static ChildIdentifier ForIndexer(ArrayIndexer indexer) => new(indexer.IdentifierProperty, JsonColumnDocuments.UnwrapConceptValue(indexer.Identifier));

    /// <summary>
    /// Check whether a stored identifier value is the one identified.
    /// </summary>
    /// <remarks>
    /// The identifier is converted to a string once, up front. The stored value is converted before the equality
    /// check when <paramref name="eagerStoredConversion"/> is set, and only when equality fails otherwise.
    /// </remarks>
    /// <param name="stored">The stored identifier value.</param>
    /// <param name="eagerStoredConversion">Whether to convert the stored value before the equality check.</param>
    /// <returns>True if it matches, false if not.</returns>
    internal bool Matches(object? stored, bool eagerStoredConversion)
    {
        if (eagerStoredConversion)
        {
            var storedAsString = stored?.ToString();
            return Equals(stored, Value) || string.Equals(storedAsString, AsString, StringComparison.Ordinal);
        }

        return Equals(stored, Value) || stored?.ToString() == AsString;
    }

    /// <summary>
    /// Find the index of the child holding this identifier.
    /// </summary>
    /// <param name="list">The children to search.</param>
    /// <param name="eagerStoredConversion">Whether to convert stored values before the equality check.</param>
    /// <returns>The index, or -1 when no child holds the identifier.</returns>
    internal int FindIndex(IList<object?> list, bool eagerStoredConversion)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (!ChildIdentity.TryGetValue(list[i], Name, out var stored))
            {
                continue;
            }

            if (Matches(stored, eagerStoredConversion))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Find the child document holding this identifier, matching stored values eagerly.
    /// </summary>
    /// <param name="elements">The children to search.</param>
    /// <returns>The child document, or null when none holds the identifier.</returns>
    internal IDictionary<string, object?>? FindDocument(IList<object?> elements) => elements
        .OfType<IDictionary<string, object?>>()
        .FirstOrDefault(parent => ChildIdentity.TryGetValue(parent, Name, out var matchValue) && Matches(matchValue, true));
}
