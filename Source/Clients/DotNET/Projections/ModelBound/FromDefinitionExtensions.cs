// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Extension methods for working with <see cref="FromDefinition"/>.
/// </summary>
static class FromDefinitionExtensions
{
    /// <summary>
    /// Adds a set mapping to the From definition for a given event type.
    /// </summary>
    /// <param name="targetFrom">The target From dictionary to add the mapping to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="eventType">The event type to map from.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    /// <param name="eventPropertyName">The property name on the event.</param>
    internal static void AddSetMapping(this IDictionary<EventType, FromDefinition> targetFrom, Func<Type, EventType> getOrCreateEventType, INamingPolicy namingPolicy, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var fromDefinition = targetFrom.GetOrCreateFromDefinition(eventTypeId);
        var eventPropertyPath = new PropertyPath(eventPropertyName);
        fromDefinition.Properties[propertyName] = namingPolicy.GetPropertyName(eventPropertyPath);
    }

    /// <summary>
    /// Adds an add mapping to the From definition for a given event type.
    /// </summary>
    /// <param name="targetFrom">The target From dictionary to add the mapping to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="eventType">The event type to map from.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    /// <param name="eventPropertyName">The property name on the event.</param>
    internal static void AddAddMapping(this IDictionary<EventType, FromDefinition> targetFrom, Func<Type, EventType> getOrCreateEventType, INamingPolicy namingPolicy, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var fromDefinition = targetFrom.GetOrCreateFromDefinition(eventTypeId);
        var eventPropertyPath = new PropertyPath(eventPropertyName);
        var convertedEventPropertyName = namingPolicy.GetPropertyName(eventPropertyPath);
        fromDefinition.Properties[propertyName] = $"{WellKnownExpressions.Add}({convertedEventPropertyName})";
    }

    /// <summary>
    /// Adds a subtract mapping to the From definition for a given event type.
    /// </summary>
    /// <param name="targetFrom">The target From dictionary to add the mapping to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="eventType">The event type to map from.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    /// <param name="eventPropertyName">The property name on the event.</param>
    internal static void AddSubtractMapping(this IDictionary<EventType, FromDefinition> targetFrom, Func<Type, EventType> getOrCreateEventType, INamingPolicy namingPolicy, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var fromDefinition = targetFrom.GetOrCreateFromDefinition(eventTypeId);
        var eventPropertyPath = new PropertyPath(eventPropertyName);
        var convertedEventPropertyName = namingPolicy.GetPropertyName(eventPropertyPath);
        fromDefinition.Properties[propertyName] = $"{WellKnownExpressions.Subtract}({convertedEventPropertyName})";
    }

    /// <summary>
    /// Adds an increment mapping to the From definition for a given event type.
    /// </summary>
    /// <param name="targetFrom">The target From dictionary to add the mapping to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="eventType">The event type to map from.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    internal static void AddIncrementMapping(this IDictionary<EventType, FromDefinition> targetFrom, Func<Type, EventType> getOrCreateEventType, Type eventType, string propertyName)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var fromDefinition = targetFrom.GetOrCreateFromDefinition(eventTypeId);
        fromDefinition.Properties[propertyName] = $"{WellKnownExpressions.Increment}()";
    }

    /// <summary>
    /// Adds a decrement mapping to the From definition for a given event type.
    /// </summary>
    /// <param name="targetFrom">The target From dictionary to add the mapping to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="eventType">The event type to map from.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    internal static void AddDecrementMapping(this IDictionary<EventType, FromDefinition> targetFrom, Func<Type, EventType> getOrCreateEventType, Type eventType, string propertyName)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var fromDefinition = targetFrom.GetOrCreateFromDefinition(eventTypeId);
        fromDefinition.Properties[propertyName] = $"{WellKnownExpressions.Decrement}()";
    }

    /// <summary>
    /// Adds a count mapping to the From definition for a given event type.
    /// </summary>
    /// <param name="targetFrom">The target From dictionary to add the mapping to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="eventType">The event type to map from.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    internal static void AddCountMapping(this IDictionary<EventType, FromDefinition> targetFrom, Func<Type, EventType> getOrCreateEventType, Type eventType, string propertyName)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var fromDefinition = targetFrom.GetOrCreateFromDefinition(eventTypeId);
        fromDefinition.Properties[propertyName] = $"{WellKnownExpressions.Count}()";
    }

    /// <summary>
    /// Adds a FromEvery mapping to the From definition for a given event type.
    /// Maps from either an event property or an event context property.
    /// </summary>
    /// <param name="targetFrom">The target From dictionary to add the mapping to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="eventType">The event type to map from.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    /// <param name="attribute">The FromEvery attribute instance.</param>
    internal static void AddFromEveryMapping(this IDictionary<EventType, FromDefinition> targetFrom, Func<Type, EventType> getOrCreateEventType, INamingPolicy namingPolicy, Type eventType, string propertyName, FromEveryAttribute attribute)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var fromDefinition = targetFrom.GetOrCreateFromDefinition(eventTypeId);

        if (!string.IsNullOrEmpty(attribute.ContextProperty))
        {
            // Map from event context property
            var contextPropertyPath = new PropertyPath(attribute.ContextProperty);
            var convertedContextPropertyName = namingPolicy.GetPropertyName(contextPropertyPath);
            fromDefinition.Properties[propertyName] = $"{WellKnownExpressions.EventContext}({convertedContextPropertyName})";
        }
        else if (!string.IsNullOrEmpty(attribute.Property))
        {
            // Map from event property
            var eventPropertyPath = new PropertyPath(attribute.Property);
            fromDefinition.Properties[propertyName] = namingPolicy.GetPropertyName(eventPropertyPath);
        }
        else
        {
            // If neither is specified, map from matching property name on event
            fromDefinition.Properties[propertyName] = propertyName;
        }
    }

    static FromDefinition GetOrCreateFromDefinition(this IDictionary<EventType, FromDefinition> targetFrom, EventType eventTypeId)
    {
        if (!targetFrom.TryGetValue(eventTypeId, out var fromDefinition))
        {
            fromDefinition = new FromDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            targetFrom[eventTypeId] = fromDefinition;
        }
        return fromDefinition;
    }
}
