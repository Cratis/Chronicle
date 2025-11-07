// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Extension methods for processing Join attributes.
/// </summary>
static class JoinDefinitionExtensions
{
    /// <summary>
    /// Processes a Join attribute and adds the join definition to the target dictionary.
    /// </summary>
    /// <param name="targetJoin">The target Join dictionary to add the definition to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="attr">The Join attribute to process.</param>
    /// <param name="eventType">The event type to join from.</param>
    /// <param name="memberName">The member name on the projection model.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    internal static void ProcessJoinAttribute(
        this IDictionary<EventType, JoinDefinition> targetJoin,
        Func<Type, EventType> getOrCreateEventType,
        INamingPolicy namingPolicy,
        Attribute attr,
        Type eventType,
        string memberName,
        string propertyName)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var onProperty = attr.GetType().GetProperty(nameof(JoinAttribute<object>.On));
        var eventPropertyNameProperty = attr.GetType().GetProperty(nameof(JoinAttribute<object>.EventPropertyName));

        var on = onProperty?.GetValue(attr) as string;
        var eventPropertyName = eventPropertyNameProperty?.GetValue(attr) as string;

        if (!targetJoin.TryGetValue(eventTypeId, out var joinDef))
        {
            joinDef = new JoinDefinition
            {
                On = on ?? propertyName,
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            targetJoin[eventTypeId] = joinDef;
        }

        var eventPropPath = new PropertyPath(eventPropertyName ?? memberName);
        joinDef.Properties[propertyName] = namingPolicy.GetPropertyName(eventPropPath);
    }
}
