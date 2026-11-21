// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Finds collection read-model properties that AutoMap maps to nothing.
/// </summary>
internal static class UnmappedCollectionFinder
{
    /// <summary>
    /// Finds collection (array) read-model properties that AutoMap to nothing — a property Chronicle expects to
    /// fill by matching an event property of the same name, but for which no source event carries a matching
    /// property and no explicit mapping exists. Such a property silently projects as an empty collection.
    /// </summary>
    /// <param name="projectionDefinition">The <see cref="ProjectionDefinition"/> for the projection level being inspected.</param>
    /// <param name="currentReadModelSchema">The <see cref="JsonSchema"/> for the read model (or child) at this level.</param>
    /// <param name="settings">The effective <see cref="AutoMapSettings"/> for this projection level.</param>
    /// <param name="eventTypeSchemas">The <see cref="EventTypeSchema">schemas</see> for the participating event types.</param>
    /// <returns>The collection properties that will always project empty; empty when everything maps.</returns>
    internal static IReadOnlyList<UnmappedCollectionProperty> Find(
        ProjectionDefinition projectionDefinition,
        JsonSchema currentReadModelSchema,
        AutoMapSettings settings,
        IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        // The silent-empty failure only happens when AutoMap is expected to wire the property by name. With
        // AutoMap disabled the developer has taken over mapping, so an unmapped collection is intentional.
        if (settings.AutoMap != AutoMap.Enabled || currentReadModelSchema is null || projectionDefinition.From.Count == 0)
        {
            return [];
        }

        var schemaList = eventTypeSchemas as IReadOnlyList<EventTypeSchema> ?? eventTypeSchemas.ToList();
        var covered = CollectCoveredPropertyNames(projectionDefinition, currentReadModelSchema, schemaList, settings);

        // Child and nested collections are materialized as their own projection structures, not by name-AutoMap.
        var structural = new HashSet<string>(
            projectionDefinition.Children.Keys.Concat(projectionDefinition.Nested?.Keys ?? []).Select(key => key.LastSegment.Value),
            StringComparer.OrdinalIgnoreCase);

        var eventTypeNames = string.Join(", ", projectionDefinition.From.Keys.Select(_ => _.Id.Value));

        return currentReadModelSchema.Properties.Values
            .Where(property => property.IsArray &&
                               !covered.Contains(property.Name) &&
                               !structural.Contains(property.Name) &&
                               !settings.NoAutoMapProperties.Contains(property.Name))
            .Select(property => new UnmappedCollectionProperty(property.Name, eventTypeNames))
            .ToList();
    }

    static HashSet<string> CollectCoveredPropertyNames(
        ProjectionDefinition projectionDefinition,
        JsonSchema currentReadModelSchema,
        IReadOnlyList<EventTypeSchema> schemaList,
        AutoMapSettings settings)
    {
        var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        JsonSchema? SchemaOf(EventType eventType) => schemaList.SchemaFor(eventType, projectionDefinition.GenerationDelivery)?.Schema;

        void CoverFrom(FromDefinition fromDefinition, EventType eventType) =>
            covered.UnionWith(AutoMapMerging.MergeFrom(fromDefinition, currentReadModelSchema, SchemaOf(eventType), settings).Select(_ => _.Key.LastSegment.Value));

        foreach (var (eventType, fromDefinition) in projectionDefinition.From)
        {
            CoverFrom(fromDefinition, eventType);
        }

        foreach (var (eventType, joinDefinition) in projectionDefinition.Join)
        {
            covered.UnionWith(AutoMapMerging.MergeJoin(joinDefinition, currentReadModelSchema, SchemaOf(eventType), settings).Select(_ => _.Key.LastSegment.Value));
        }

        covered.UnionWith(projectionDefinition.FromEvery.Properties.Keys.Select(key => key.LastSegment.Value));

        var derivedFroms = (projectionDefinition.FromDerivatives ?? []).SelectMany(derivative => derivative.EventTypes.Select(eventType => (EventType: eventType, derivative.From)));
        foreach (var (eventType, fromDefinition) in derivedFroms)
        {
            CoverFrom(fromDefinition, eventType);
        }

        return covered;
    }
}
