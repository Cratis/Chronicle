// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Evaluates projection definition changes for replay recommendations.
/// </summary>
internal static class ProjectionReplayRecommendationEvaluator
{
    /// <summary>
    /// Gets added event types if those are the only relevant changes in a projection definition.
    /// </summary>
    /// <param name="previousDefinition">The previous definition.</param>
    /// <param name="currentDefinition">The current definition.</param>
    /// <param name="objectComparer">The <see cref="IObjectComparer"/> to use for comparisons.</param>
    /// <returns>Added event types when only event types are added; otherwise an empty collection.</returns>
    public static EventType[] GetAddedEventTypesIfOnlyEventTypesChanged(
        ProjectionDefinition previousDefinition,
        ProjectionDefinition currentDefinition,
        IObjectComparer objectComparer)
    {
        var previousEventTypes = GetEventTypes(previousDefinition);
        var currentEventTypes = GetEventTypes(currentDefinition);

        var addedEventTypes = currentEventTypes.Except(previousEventTypes).ToArray();
        if (addedEventTypes.Length == 0)
        {
            return [];
        }

        if (previousEventTypes.Except(currentEventTypes).Any())
        {
            return [];
        }

        var eventTypesToExclude = addedEventTypes.ToHashSet();
        var normalizedPreviousDefinition = NormalizeForComparison(previousDefinition);
        var normalizedCurrentDefinition = NormalizeForComparison(RemoveEventTypes(currentDefinition, eventTypesToExclude));

        return objectComparer.Compare(normalizedPreviousDefinition, normalizedCurrentDefinition, ObjectComparerMode.Loose, out _)
            ? addedEventTypes
            : [];
    }

    static HashSet<EventType> GetEventTypes(ProjectionDefinition definition)
    {
        var eventTypes = new HashSet<EventType>();

        AddEventTypes(eventTypes, definition.From.Keys);
        AddEventTypes(eventTypes, definition.Join.Keys);
        AddEventTypes(eventTypes, definition.RemovedWith.Keys);
        AddEventTypes(eventTypes, definition.RemovedWithJoin.Keys);
        AddEventTypes(eventTypes, definition.FromDerivatives.SelectMany(_ => _.EventTypes));

        foreach (var child in definition.Children.Values)
        {
            AddEventTypes(eventTypes, GetEventTypes(child));
        }

        if (definition.Nested is not null)
        {
            foreach (var nested in definition.Nested.Values)
            {
                AddEventTypes(eventTypes, GetEventTypes(nested));
            }
        }

        return eventTypes;
    }

    static void AddEventTypes(HashSet<EventType> target, IEnumerable<EventType> source)
        => target.UnionWith(source);

    static ProjectionDefinition RemoveEventTypes(
        ProjectionDefinition definition,
        HashSet<EventType> eventTypesToExclude) =>
        definition with
        {
            From = FilterByEventType(definition.From, eventTypesToExclude),
            Join = FilterByEventType(definition.Join, eventTypesToExclude),
            RemovedWith = FilterByEventType(definition.RemovedWith, eventTypesToExclude),
            RemovedWithJoin = FilterByEventType(definition.RemovedWithJoin, eventTypesToExclude),
            FromDerivatives = definition.FromDerivatives
                .Select(_ => new FromDerivatives(_.EventTypes.Where(eventType => !eventTypesToExclude.Contains(eventType)).ToArray(), _.From))
                .Where(_ => _.EventTypes.Any())
                .ToArray(),
            Children = definition.Children.ToDictionary(_ => _.Key, _ => RemoveEventTypes(_.Value, eventTypesToExclude)),
            Nested = definition.Nested?.ToDictionary(_ => _.Key, _ => RemoveEventTypes(_.Value, eventTypesToExclude))
        };

    static ChildrenDefinition RemoveEventTypes(
        ChildrenDefinition definition,
        HashSet<EventType> eventTypesToExclude) =>
        definition with
        {
            From = FilterByEventType(definition.From, eventTypesToExclude),
            Join = FilterByEventType(definition.Join, eventTypesToExclude),
            RemovedWith = FilterByEventType(definition.RemovedWith, eventTypesToExclude),
            RemovedWithJoin = FilterByEventType(definition.RemovedWithJoin, eventTypesToExclude),
            Children = definition.Children.ToDictionary(_ => _.Key, _ => RemoveEventTypes(_.Value, eventTypesToExclude)),
            Nested = definition.Nested?.ToDictionary(_ => _.Key, _ => RemoveEventTypes(_.Value, eventTypesToExclude))
        };

    static Dictionary<EventType, TDefinition> FilterByEventType<TDefinition>(
        IDictionary<EventType, TDefinition> source,
        HashSet<EventType> eventTypesToExclude)
        where TDefinition : class =>
        source
            .Where(_ => !eventTypesToExclude.Contains(_.Key))
            .ToDictionary(_ => _.Key, _ => _.Value);

    static ProjectionDefinition NormalizeForComparison(ProjectionDefinition definition) =>
        definition with
        {
            ReadModel = null!,
            InitialModelState = null!,
            LastUpdated = null
        };
}
