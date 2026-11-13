// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.Operator;

/// <summary>
/// Represents the registered constraints selected for an explicit reindex, or a refusal reason.
/// </summary>
/// <param name="Definitions">The applicable rebuildable definitions.</param>
/// <param name="Error">The refusal reason, when selection failed.</param>
internal record ConstraintReindexSelection(IConstraintDefinition[] Definitions, string? Error)
{
    /// <summary>
    /// Selects the requested definitions without silently discarding unsupported names.
    /// </summary>
    /// <param name="command">The operator request.</param>
    /// <param name="definitions">The currently registered definitions.</param>
    /// <returns>The selection or its refusal reason.</returns>
    internal static ConstraintReindexSelection For(ReindexConstraints command, IReadOnlyCollection<IConstraintDefinition> definitions)
    {
        var names = command.ConstraintNames.ToHashSet(StringComparer.Ordinal);
        var selected = definitions.Where(definition => names.Count == 0
            ? definition is UniqueConstraintDefinition or ClosesStreamConstraintDefinition
            : names.Contains(definition.Name.Value)).ToArray();
        if (names.Except(selected.Select(definition => definition.Name.Value)).Any())
        {
            return new([], "One or more constraint names are not registered.");
        }

        if (selected.Any(definition => definition is not UniqueConstraintDefinition and not ClosesStreamConstraintDefinition))
        {
            return new([], "Only unique and closing constraints can be reindexed.");
        }

        if (names.Count > 0 && selected.Any(definition => !definition.AppliesTo(command.EventSequenceId)))
        {
            return new([], "One or more constraints do not apply to the event sequence.");
        }

        var applicable = selected.Where(definition => definition.AppliesTo(command.EventSequenceId)).ToArray();
        return applicable.Length == 0 ? new([], "No applicable constraints can be reindexed.") : new(applicable, null);
    }
}
