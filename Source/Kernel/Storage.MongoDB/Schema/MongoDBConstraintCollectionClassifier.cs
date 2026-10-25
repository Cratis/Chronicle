// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.MongoDB.Schema;

/// <summary>
/// Classifies existing constraint collections using stored definitions rather than ambiguous suffixes.
/// </summary>
internal static class MongoDBConstraintCollectionClassifier
{
    /// <summary>
    /// Finds a single matching retention mode, including scoped and unscoped collection names.
    /// </summary>
    /// <param name="collection">The existing collection name.</param>
    /// <param name="definitions">The event store's current unique constraint definitions.</param>
    /// <param name="sequences">Known sequence names in the namespace.</param>
    /// <returns>The matching mode, or null when no definition matches or modes disagree.</returns>
    internal static UniqueConstraintMode? Classify(string collection, IReadOnlyList<UniqueConstraintDefinition> definitions, IReadOnlyList<string> sequences)
    {
        var candidates = sequences.Where(sequence => collection.StartsWith($"{sequence}+", StringComparison.Ordinal)).ToArray();
        if (candidates.Length == 0)
        {
            var separator = collection.IndexOf('+', StringComparison.Ordinal);
            if (separator <= 0) return null;
            candidates = [collection[..separator]];
        }
        var modes = definitions.Where(definition => (definition.Mode is UniqueConstraintMode.PerEventSource or UniqueConstraintMode.PerValue)
                && candidates.Any(sequence => definition.AppliesTo(sequence) && Matches(collection, sequence, definition)))
            .Select(definition => definition.Mode).Distinct().ToArray();
        return modes.Length == 1 ? modes[0] : null;
    }

    static bool Matches(string collection, string sequence, UniqueConstraintDefinition definition)
    {
        var prefix = $"{sequence}+{definition.Name}+";
        if (!collection.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var tail = collection[prefix.Length..];
        var suffix = definition.Mode == UniqueConstraintMode.PerValue ? "values+constraint" : "constraint";
        return tail == suffix || (tail.Length > suffix.Length + 1 && tail.EndsWith($"+{suffix}", StringComparison.Ordinal));
    }
}
