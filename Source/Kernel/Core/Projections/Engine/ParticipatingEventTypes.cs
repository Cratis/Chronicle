// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Finds the event types a projection definition takes part in, and the schemas that belong to them.
/// </summary>
internal static class ParticipatingEventTypes
{
    /// <summary>
    /// Selects the schemas a plan for a definition is compiled from.
    /// </summary>
    /// <param name="definition">The <see cref="ProjectionDefinition"/>.</param>
    /// <param name="schemas">The schemas of every event type of the event store.</param>
    /// <returns>
    /// The schemas of the event types the definition names, in the order they were given. A projection that subscribes to all events,
    /// or one that targets a graph, can see any event type and keeps every schema.
    /// </returns>
    internal static IReadOnlyList<EventTypeSchema> SchemasFor(ProjectionDefinition definition, IReadOnlyList<EventTypeSchema> schemas)
    {
        if (definition.SubscribesToAllEvents || definition.Graph is not null)
        {
            return schemas;
        }

        var ids = new HashSet<EventTypeId>();
        Collect(definition, ids);
        return [.. schemas.Where(schema => ids.Contains(schema.Type.Id))];
    }

    static void Collect(ProjectionDefinition definition, HashSet<EventTypeId> ids)
    {
        ids.UnionWith(definition.From.Keys.Select(_ => _.Id));
        ids.UnionWith(definition.Join.Keys.Select(_ => _.Id));
        ids.UnionWith(definition.RemovedWith.Keys.Select(_ => _.Id));
        ids.UnionWith(definition.RemovedWithJoin.Keys.Select(_ => _.Id));
        foreach (var derivatives in definition.FromDerivatives ?? [])
        {
            ids.UnionWith(derivatives.EventTypes.Select(_ => _.Id));
        }

        if (definition.FromEventProperty is not null)
        {
            ids.Add(definition.FromEventProperty.Event.Id);
        }

        foreach (var child in definition.Children.Values)
        {
            Collect(child, ids);
        }

        foreach (var nested in definition.Nested?.Values ?? [])
        {
            Collect(nested, ids);
        }
    }
}
