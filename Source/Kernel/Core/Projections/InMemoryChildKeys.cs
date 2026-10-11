// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Binds in-memory child operations to the identifier type stored on the child.
/// </summary>
internal static class InMemoryChildKeys
{
    /// <summary>
    /// Rebinds array indexers without changing the root the event resolved to or creating missing children.
    /// </summary>
    /// <remarks>
    /// Event-sequence key resolution may return a string for an identifier stored as a Guid. The persistent sinks
    /// convert through the schema; the in-memory engine must bind before looking up, updating or removing a child.
    /// Rebinding against this root also works when another root holds a child with the same identifier.
    /// </remarks>
    /// <param name="key">The resolved key.</param>
    /// <param name="state">The existing root state.</param>
    /// <returns>The key using the stored child identifiers.</returns>
    public static Key Bind(Key key, ExpandoObject state)
    {
        var indexers = new List<ArrayIndexer>();
        foreach (var indexer in key.ArrayIndexers.All)
        {
            var identifier = FindIdentifier(state, indexer, new ArrayIndexers(indexers));
            indexers.Add(indexer with { Identifier = identifier ?? indexer.Identifier });
        }

        return key with { ArrayIndexers = new ArrayIndexers(indexers) };
    }

    static object? FindIdentifier(ExpandoObject state, ArrayIndexer indexer, ArrayIndexers ancestors) =>
        indexer.IdentifierProperty.IsSet
            ? FindChildren(state, indexer, ancestors)
                .Select(child => indexer.IdentifierProperty.GetValue(child, ArrayIndexers.NoIndexers))
                .FirstOrDefault(identifier => identifier is not null && string.Equals(identifier.ToString(), indexer.Identifier.ToString(), StringComparison.Ordinal))
            : null;

    static IEnumerable<object> FindChildren(ExpandoObject state, ArrayIndexer indexer, ArrayIndexers ancestors)
    {
        if (state.TryGetExistingPath(indexer.ArrayProperty, ancestors) is not IDictionary<string, object?> parent)
        {
            return [];
        }

        return parent.TryGetValue(indexer.ArrayProperty.LastSegment.Value, out var collection) && collection is IEnumerable<object> children
            ? children
            : [];
    }
}
