// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Linq;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Applies a projection level's every-event mappings to events handled only by its descendants.
/// </summary>
internal static class ChildEveryEventMappings
{
    /// <summary>
    /// Enumerates descendant-owned event types that are not handled at this projection level.
    /// </summary>
    /// <param name="projection">The projection level whose child-only events to collect.</param>
    /// <returns>The distinct child-only event types and their owning projections.</returns>
    internal static IEnumerable<(IProjection Projection, EventType EventType)> GetChildOnlyEventTypes(IProjection projection)
    {
        var mappedEventTypeIds = projection.OwnEventTypes.Select(_ => _.Id).ToHashSet();
        return Descendants(projection)
            .SelectMany(child => child.OwnEventTypes.Select(eventType => (Projection: child, EventType: eventType)))
            .Where(entry => mappedEventTypeIds.Add(entry.EventType.Id));
    }

    /// <summary>
    /// Gets the child-only event types the every-event mappings subscribe to.
    /// </summary>
    /// <param name="projection">The projection level whose child-only events to collect.</param>
    /// <param name="definition">The every-event definition controlling child inclusion.</param>
    /// <returns>The child-only event types, or none when children are excluded.</returns>
    internal static IEnumerable<EventType> GetIncludedChildEventTypes(IProjection projection, FromEveryDefinition definition) =>
        definition.IncludeChildren ? GetChildOnlyEventTypes(projection).Select(_ => _.EventType) : [];

    /// <summary>
    /// Subscribes this level's every-event mappings to its child-only events.
    /// </summary>
    /// <param name="projection">The projection level receiving the mappings.</param>
    /// <param name="definition">The every-event definition controlling child inclusion.</param>
    /// <param name="propertyMappers">The property mappers targeting this level.</param>
    internal static void Subscribe(
        Projection projection,
        FromEveryDefinition definition,
        IReadOnlyList<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappers)
    {
        if (!definition.IncludeChildren || propertyMappers.Count == 0)
        {
            return;
        }

        foreach (var (child, eventType) in GetChildOnlyEventTypes(projection))
        {
            // Event contexts already carry the descendant's resolved parent hierarchy. Reuse that key and
            // its array indexers rather than introducing a new root resolver based on the child's identifier.
            // Descendant events provided outside top-down handling (a resolved future) reach these mappings too.
            var observable = projection.EventIncludingDescendantEvents.WhereEventTypeEquals(eventType);
            if (child.GetOperationTypeFor(eventType).HasFlag(ProjectionOperationType.Join))
            {
                // A child join can reach several roots. Keep parent mappings inside that same join boundary,
                // never in a direct write under the join source's key.
                observable = observable.Join(child.ChildrenPropertyPath + child.IdentifiedByProperty);
            }

            // Unlike projecting a child creation, ancestor mappings must also run when the key targets a
            // deeper collection. Their full property paths select the containing ancestor through its indexer.
            projection.Subscriptions.Add(observable.Subscribe(context =>
                context.Changeset.SetProperties(propertyMappers, IndexersReaching(projection.ChildrenPropertyPath, context.Key.ArrayIndexers))));
        }
    }

    /// <summary>
    /// Keeps only the array indexers that locate the projection level itself, dropping those of its descendants.
    /// </summary>
    /// <param name="levelPath">The children property path of the projection level the mappings target.</param>
    /// <param name="arrayIndexers">The array indexers of the descendant's resolved key.</param>
    /// <returns>The <see cref="ArrayIndexers"/> reaching the projection level.</returns>
    /// <remarks>
    /// A change carrying the descendant's own indexer reads as a change to that descendant: when the same event adds the
    /// child, the changeset folds it into the added child rather than the level it was mapped for.
    /// </remarks>
    static ArrayIndexers IndexersReaching(PropertyPath levelPath, ArrayIndexers arrayIndexers)
    {
        var levelSegments = levelPath.Segments.Select(_ => _.Value).ToArray();
        return new ArrayIndexers(arrayIndexers.All.Where(indexer =>
        {
            var indexerSegments = indexer.ArrayProperty.Segments.Select(_ => _.Value).ToArray();
            return indexerSegments.Length <= levelSegments.Length && indexerSegments.SequenceEqual(levelSegments.Take(indexerSegments.Length));
        }).ToArray());
    }

    static IEnumerable<IProjection> Descendants(IProjection projection) =>
        projection.ChildProjections.SelectMany(child => new[] { child }.Concat(Descendants(child)));
}
