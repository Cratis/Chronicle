// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Normalizes legacy bare EventSourceId keys when no matching payload property exists.
/// </summary>
internal static partial class LegacyEventSourceIdKeys
{
    /// <summary>
    /// Creates a runtime definition with unambiguous legacy keys normalized, without changing the stored definition.
    /// </summary>
    /// <param name="definition">The stored projection definition.</param>
    /// <param name="eventTypeSchemas">The known event type schemas.</param>
    /// <param name="rewrittenEventTypes">Receives event types whose keys were normalized.</param>
    /// <returns>The runtime definition, or the original definition when no keys need normalization.</returns>
    internal static ProjectionDefinition Normalize(
        ProjectionDefinition definition,
        IReadOnlyList<EventTypeSchema> eventTypeSchemas,
        ICollection<EventType> rewrittenEventTypes)
    {
        var from = NormalizeDictionary(definition.From, (type, value) => NormalizeFrom(value, [type], eventTypeSchemas, rewrittenEventTypes));
        var join = NormalizeDictionary(definition.Join, (type, value) =>
        {
            var key = NormalizeKey(value.Key, [type], eventTypeSchemas, rewrittenEventTypes);
            return key == value.Key ? value : value with { Key = key! };
        });
        var removedWith = NormalizeDictionary(definition.RemovedWith, (type, value) =>
        {
            var key = NormalizeKey(value.Key, [type], eventTypeSchemas, rewrittenEventTypes);
            var parentKey = NormalizeKey(value.ParentKey, [type], eventTypeSchemas, rewrittenEventTypes);
            return key == value.Key && parentKey == value.ParentKey ? value : value with { Key = key!, ParentKey = parentKey };
        });
        var removedWithJoin = NormalizeDictionary(definition.RemovedWithJoin, (type, value) =>
        {
            var key = NormalizeKey(value.Key, [type], eventTypeSchemas, rewrittenEventTypes);
            return key == value.Key ? value : value with { Key = key! };
        });
        var children = NormalizeDictionary(definition.Children, (_, child) => (ChildrenDefinition)Normalize(child, eventTypeSchemas, rewrittenEventTypes));
        var nested = definition.Nested is null ? null : NormalizeDictionary(definition.Nested, (_, child) => (ChildrenDefinition)Normalize(child, eventTypeSchemas, rewrittenEventTypes));
        var derivatives = definition.FromDerivatives.Select(derivative =>
        {
            var normalized = NormalizeFrom(derivative.From, derivative.EventTypes.ToArray(), eventTypeSchemas, rewrittenEventTypes);
            return ReferenceEquals(normalized, derivative.From) ? derivative : derivative with { From = normalized };
        }).ToArray();

        if (ReferenceEquals(from, definition.From) && ReferenceEquals(join, definition.Join) &&
            ReferenceEquals(removedWith, definition.RemovedWith) && ReferenceEquals(removedWithJoin, definition.RemovedWithJoin) &&
            ReferenceEquals(children, definition.Children) && ReferenceEquals(nested, definition.Nested) &&
            derivatives.SequenceEqual(definition.FromDerivatives))
        {
            return definition;
        }

        // Record copying preserves a child's runtime type and its IdentifiedBy / All members.
        return definition with
        {
            From = from,
            Join = join,
            RemovedWith = removedWith,
            RemovedWithJoin = removedWithJoin,
            Children = children,
            Nested = nested,
            FromDerivatives = derivatives
        };
    }

    /// <summary>
    /// Logs a compatibility rewrite without including event content.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="projectionId">The projection identifier.</param>
    /// <param name="eventTypeId">The event type identifier.</param>
    [LoggerMessage(LogLevel.Information, "Projection {ProjectionId}: key 'EventSourceId' for event type {EventTypeId} is not a property of the event; resolving it as the event source id")]
    internal static partial void LogRewritten(ILogger logger, string projectionId, string eventTypeId);

    static FromDefinition NormalizeFrom(
        FromDefinition definition,
        IReadOnlyList<EventType> eventTypes,
        IReadOnlyList<EventTypeSchema> schemas,
        ICollection<EventType> rewritten)
    {
        var key = NormalizeKey(definition.Key, eventTypes, schemas, rewritten);
        var parentKey = NormalizeKey(definition.ParentKey, eventTypes, schemas, rewritten);
        return key == definition.Key && parentKey == definition.ParentKey
            ? definition
            : definition with { Key = key!, ParentKey = parentKey };
    }

    static PropertyExpression? NormalizeKey(
        PropertyExpression? key,
        IReadOnlyList<EventType> eventTypes,
        IReadOnlyList<EventTypeSchema> schemas,
        ICollection<EventType> rewritten)
    {
        if (key is null || !string.Equals(key.Value, "EventSourceId", StringComparison.OrdinalIgnoreCase) ||
            eventTypes.Count == 0 || !eventTypes.All(type => CanRewrite(type, key.Value, schemas)))
        {
            return key;
        }

        foreach (var type in eventTypes)
        {
            rewritten.Add(type);
        }

        return new PropertyExpression(WellKnownExpressions.EventSourceId);
    }

    static bool CanRewrite(EventType type, string property, IReadOnlyList<EventTypeSchema> schemas)
    {
        var generations = schemas.Where(schema => schema.Type.Id == type.Id).ToArray();

        // Stored events may carry the content of any generation up to the latest, and a pinned projection reads the
        // generation it names. Rewrite only when every generation from the first to the highest is known.
        var known = generations.Select(schema => schema.Type.Generation.Value).ToHashSet();
        var highest = known.Count == 0 ? 0U : known.Max();
        var complete = highest != uint.MaxValue && known.Count == highest &&
            Enumerable.Range(1, (int)highest).All(generation => known.Contains((uint)generation)) &&
            (type.Generation == EventTypeGeneration.Unspecified || type.Generation.Value <= highest);

        // ActualSchema selects one composition branch; it cannot rule out a property in another branch.
        return complete && generations.All(schema =>
            !schema.Schema.HasReference && schema.Schema.AllOf.Count == 0 && schema.Schema.AnyOf.Count == 0 && schema.Schema.OneOf.Count == 0 &&
            !schema.Schema.ActualSchema.ActualProperties.Keys.Any(name => string.Equals(name, property, StringComparison.Ordinal)));
    }

    static IDictionary<TKey, TValue> NormalizeDictionary<TKey, TValue>(IDictionary<TKey, TValue> definitions, Func<TKey, TValue, TValue> normalize)
        where TKey : notnull
        where TValue : class
    {
        Dictionary<TKey, TValue>? changed = null;
        foreach (var (key, value) in definitions)
        {
            var normalized = normalize(key, value);
            if (!ReferenceEquals(normalized, value))
            {
                changed ??= new(definitions);
                changed[key] = normalized;
            }
        }

        return changed ?? definitions;
    }
}
