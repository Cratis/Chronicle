// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Extension methods for processing RemovedWith attributes.
/// </summary>
static class RemovedWithExtensions
{
    /// <summary>
    /// Processes a RemovedWith attribute and adds the removed with definition to the target dictionary.
    /// </summary>
    /// <param name="targetRemovedWith">The target RemovedWith dictionary to add the definition to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/> used to convert event property names to their serialized form.</param>
    /// <param name="attr">The RemovedWith attribute to process.</param>
    /// <param name="eventType">The event type that triggers removal.</param>
    internal static void ProcessRemovedWithAttribute(
        this IDictionary<EventType, RemovedWithDefinition> targetRemovedWith,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        Attribute attr,
        Type eventType)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var keyProperty = attr.GetType().GetProperty(nameof(RemovedWithAttribute<object>.Key));
        var parentKeyProperty = attr.GetType().GetProperty(nameof(RemovedWithAttribute<object>.ParentKey));

        var key = keyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;
        var parentKey = parentKeyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;

        targetRemovedWith[eventTypeId] = new RemovedWithDefinition
        {
            Key = ApplyNamingPolicy(namingPolicy, key),
            ParentKey = ApplyNamingPolicy(namingPolicy, parentKey)
        };
    }

    /// <summary>
    /// Processes a RemovedWithJoin attribute and adds the removed with join definition to the target dictionary.
    /// </summary>
    /// <param name="targetRemovedWithJoin">The target RemovedWithJoin dictionary to add the definition to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/> used to convert event property names to their serialized form.</param>
    /// <param name="attr">The RemovedWithJoin attribute to process.</param>
    /// <param name="eventType">The event type that triggers removal.</param>
    internal static void ProcessRemovedWithJoinAttribute(
        this IDictionary<EventType, RemovedWithJoinDefinition> targetRemovedWithJoin,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        Attribute attr,
        Type eventType)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var keyProperty = attr.GetType().GetProperty(nameof(RemovedWithJoinAttribute<object>.Key));

        var key = keyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;

        targetRemovedWithJoin[eventTypeId] = new RemovedWithJoinDefinition
        {
            Key = ApplyNamingPolicy(namingPolicy, key)
        };
    }

    /// <summary>
    /// Applies the naming policy to a key expression so it matches the serialized event property name, unless the
    /// expression is a well-known projection expression (e.g. <c>$eventSourceId</c>) which must be left untouched.
    /// </summary>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/> to apply.</param>
    /// <param name="expression">The key expression to convert.</param>
    /// <returns>The converted key expression.</returns>
    static string ApplyNamingPolicy(INamingPolicy namingPolicy, string expression) =>
        expression.StartsWith('$') ? expression : namingPolicy.GetPropertyName(new PropertyPath(expression));
}
