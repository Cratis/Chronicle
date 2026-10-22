// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Preserves ordered definitions when legacy clients submit flat paths.
/// </summary>
internal static class ReadModelIndexUpdates
{
    /// <summary>
    /// Converts a contract definition using its legacy fallback when needed.
    /// </summary>
    /// <param name="index">The contract definition.</param>
    /// <returns>The complete index definition.</returns>
    internal static IndexDefinition Convert(Contracts.ReadModels.IndexDefinition index) => new(index.PropertyPath)
    {
        Properties = index.Properties is { Count: > 0 }
            ? [.. index.Properties.Select(property => new IndexedProperty(property.Path, property.Descending))]
            : [new(index.PropertyPath)]
    };

    /// <summary>
    /// Applies ordered declarations or merges a legacy flat update.
    /// </summary>
    /// <param name="incoming">The incoming declarations.</param>
    /// <param name="existing">The stored definitions.</param>
    /// <returns>The definitions to save.</returns>
    internal static IndexDefinition[] Apply(IEnumerable<Contracts.ReadModels.IndexDefinition> incoming, IEnumerable<IndexDefinition> existing)
    {
        var declarations = incoming.ToArray();
        var converted = declarations.Select(Convert);
        if (declarations.Any(index => index.Properties is { Count: > 0 })) return converted.ToArray();

        // Legacy flat-path editors cannot represent ordered definitions: keep those rather than flattening or erasing them.
        var retained = existing.Where(index => index.IsComposite || index.Properties[0].Descending).ToArray();
        var leadingPaths = retained.Select(index => index.Properties[0].Path).ToHashSet();
        return converted.Where(index => !leadingPaths.Contains(index.PropertyPath)).Concat(retained).ToArray();
    }
}
