// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>Answers the root initialization question without constructing an observable projection graph.</summary>
internal static class ProjectionDefinitionInitialization
{
    /// <summary>Identifies a root which has children but no root-creating operation.</summary>
    /// <param name="definition">The registered projection definition.</param>
    /// <returns>Whether uninitialized child-only roots should be visible.</returns>
    internal static bool IsChildOnly(ProjectionDefinition definition)
    {
        if (definition.Graph is not null || definition.SubscribesToAllEvents || definition.Children.Count == 0) return false;

        // Mirror the root operation map in ProjectionFactory: From joined with Join is not a creator.
        // Legacy normalizers change keys and every-event mappings, not these registrations or child count.
        var creators = definition.From.Keys
            .Concat((definition.FromDerivatives ?? []).SelectMany(derivative => derivative.EventTypes))
            .Concat(definition.FromEventProperty is null ? [] : new[] { definition.FromEventProperty.Event })
            .ToHashSet();
        var joins = definition.Join.Keys.ToHashSet();
        AddNestedOperations(definition.Nested?.Values ?? [], creators, joins);
        return !creators.Except(joins).Any();
    }

    static void AddNestedOperations(IEnumerable<ChildrenDefinition> nested, HashSet<EventType> creators, HashSet<EventType> joins)
    {
        foreach (var definition in nested)
        {
            creators.UnionWith(definition.From.Keys);

            // Nested RemovedWith contributes From in the existing compiled root operation map.
            creators.UnionWith(definition.RemovedWith.Keys);
            joins.UnionWith(definition.Join.Keys);
            AddNestedOperations(definition.Nested?.Values ?? [], creators, joins);
        }
    }
}
