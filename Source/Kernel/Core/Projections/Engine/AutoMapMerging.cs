// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Merges explicit property mappings with the event properties that AutoMap maps by name.
/// </summary>
internal static class AutoMapMerging
{
    static readonly string[] _aggregateExpressions =
    [
        WellKnownExpressions.Count,
        WellKnownExpressions.Increment,
        WellKnownExpressions.Decrement,
        WellKnownExpressions.Add,
        WellKnownExpressions.Subtract
    ];

    /// <summary>
    /// Combines explicit from mappings with matching event properties when AutoMap permits them.
    /// </summary>
    /// <param name="fromDefinition">The from definition.</param>
    /// <param name="currentReadModelSchema">The schema being projected into.</param>
    /// <param name="eventSchema">The event schema, if known.</param>
    /// <param name="settings">The effective <see cref="AutoMapSettings"/>.</param>
    /// <returns>The effective from property mappings.</returns>
    internal static List<KeyValuePair<PropertyPath, string>> MergeFrom(FromDefinition fromDefinition, JsonSchema currentReadModelSchema, JsonSchema? eventSchema, AutoMapSettings settings)
    {
        var merged = fromDefinition.Properties.ToList();

        // An event subscribed only to be aggregated does not name-AutoMap its other properties, so an
        // unrelated same-named property cannot silently overwrite an explicitly sourced one. The developer
        // can still opt a specific property out of AutoMap for the non-aggregate cases with [NoAutoMap].
        if (!CanAutoMap(settings.AutoMap, currentReadModelSchema, eventSchema) || IsAggregateOnly(fromDefinition))
        {
            return merged;
        }

        var readModelNames = LastSegmentsOf(merged);
        AddNameMatched(merged, eventSchema!, currentReadModelSchema, settings.NoAutoMapProperties, readModelNames, null);
        return merged;
    }

    /// <summary>
    /// Combines explicit join mappings with matching event properties when AutoMap permits them.
    /// </summary>
    /// <param name="joinDefinition">The event-specific join definition.</param>
    /// <param name="currentReadModelSchema">The schema being projected into.</param>
    /// <param name="eventSchema">The joined event's schema, if available.</param>
    /// <param name="settings">The inherited <see cref="AutoMapSettings"/>.</param>
    /// <returns>The effective join property mappings.</returns>
    internal static List<KeyValuePair<PropertyPath, string>> MergeJoin(JoinDefinition joinDefinition, JsonSchema currentReadModelSchema, JsonSchema? eventSchema, AutoMapSettings settings)
    {
        var merged = joinDefinition.Properties.ToList();
        var autoMap = joinDefinition.AutoMap == AutoMap.Inherit ? settings.AutoMap : joinDefinition.AutoMap;
        if (!CanAutoMap(autoMap, currentReadModelSchema, eventSchema))
        {
            return merged;
        }

        var readModelNames = LastSegmentsOf(merged);
        var eventNames = new HashSet<string>(merged.Select(_ => _.Value), StringComparer.OrdinalIgnoreCase);
        AddNameMatched(merged, eventSchema!, currentReadModelSchema, settings.NoAutoMapProperties, readModelNames, eventNames);
        return merged;
    }

    /// <summary>
    /// Determines whether an event is subscribed only to be aggregated — every property it maps is a
    /// count, increment, decrement, add or subtract operation and nothing else.
    /// </summary>
    /// <param name="fromDefinition">The <see cref="FromDefinition"/> to inspect.</param>
    /// <returns><see langword="true"/> if the event is subscribed only for an aggregate; otherwise <see langword="false"/>.</returns>
    static bool IsAggregateOnly(FromDefinition fromDefinition) =>
        fromDefinition.Properties.Count > 0 &&
        fromDefinition.Properties.Values.All(expression =>
            _aggregateExpressions.Any(aggregate => expression.StartsWith(aggregate, StringComparison.Ordinal)));

    static bool CanAutoMap(AutoMap autoMap, JsonSchema currentReadModelSchema, JsonSchema? eventSchema) =>
        autoMap != AutoMap.Disabled && eventSchema is not null && currentReadModelSchema is not null;

    static HashSet<string> LastSegmentsOf(List<KeyValuePair<PropertyPath, string>> merged) =>
        new(merged.Select(_ => _.Key.LastSegment.Value), StringComparer.OrdinalIgnoreCase);

    static void AddNameMatched(
        List<KeyValuePair<PropertyPath, string>> merged,
        JsonSchema eventSchema,
        JsonSchema currentReadModelSchema,
        IReadOnlySet<string> noAutoMapProperties,
        HashSet<string> readModelNames,
        HashSet<string>? eventNames)
    {
        var candidates = eventSchema.Properties.Values
            .Where(property => !readModelNames.Contains(property.Name) && eventNames?.Contains(property.Name) != true);

        foreach (var eventProperty in candidates)
        {
            var target = FindTarget(eventProperty, currentReadModelSchema, noAutoMapProperties);
            if (target is null)
            {
                continue;
            }

            merged.Add(new(new PropertyPath(target.Name), eventProperty.Name));
            readModelNames.Add(target.Name);
            eventNames?.Add(eventProperty.Name);
        }
    }

    /// <summary>
    /// Finds the read model property an event property auto-maps onto. A property flagged with [NoAutoMap] is only
    /// ever set from its explicit mapping, so an event carrying an identically named property must not auto-map onto it.
    /// </summary>
    /// <param name="eventProperty">The event property to find a target for.</param>
    /// <param name="currentReadModelSchema">The schema being projected into.</param>
    /// <param name="noAutoMapProperties">Properties excluded from automatic mapping.</param>
    /// <returns>The matching read model property, or null when there is none or it is excluded.</returns>
    static JsonSchemaProperty? FindTarget(JsonSchemaProperty eventProperty, JsonSchema currentReadModelSchema, IReadOnlySet<string> noAutoMapProperties)
    {
        var match = currentReadModelSchema.Properties.Values
            .FirstOrDefault(property => property.Name.Equals(eventProperty.Name, StringComparison.OrdinalIgnoreCase));

        return match is not null && !noAutoMapProperties.Contains(match.Name) ? match : null;
    }
}
