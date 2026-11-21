// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Decides from a <see cref="ProjectionDefinition"/> whether a global projection holds nothing but property mappings.
/// </summary>
/// <remarks>
/// Only such a projection is written with optimistic revision checks. Joins, children and removals write several documents
/// or parts of one in separate operations, which a single revision check cannot cover.
/// </remarks>
public static class FencedGlobalEligibility
{
    /// <summary>
    /// Gets the reason a definition rules out fenced writes.
    /// </summary>
    /// <param name="definition">The <see cref="ProjectionDefinition"/> to inspect.</param>
    /// <returns>The reason, or null when the definition holds only property mappings.</returns>
    public static string? ReasonFor(ProjectionDefinition definition) =>
        Reason(definition.Join.Count, definition.Children.Count, definition.RemovedWith.Count, definition.RemovedWithJoin.Count, definition.Nested)
        ?? (definition.Graph is null ? null : "it targets a graph");

    static string? Reason(int joins, int children, int removals, int removalJoins, IDictionary<Properties.PropertyPath, ChildrenDefinition>? nested)
    {
        if (joins > 0 || removalJoins > 0)
        {
            return "it joins events";
        }

        if (children > 0)
        {
            return "it has children";
        }

        if (removals > 0)
        {
            return "it removes with events";
        }

        return nested?.Values
            .Select(_ => Reason(_.Join.Count, _.Children.Count, _.RemovedWith.Count, _.RemovedWithJoin.Count, _.Nested))
            .FirstOrDefault(_ => _ is not null);
    }
}
