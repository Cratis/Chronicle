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
    /// Adds a set mapping to the From definition for a given event type with an optional key.
    /// </summary>
    /// <param name="targetFrom">The target From dictionary to add the mapping to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="eventType">The event type to map from.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    /// <param name="eventPropertyName">The property name on the event.</param>
    /// <param name="key">Optional key expression to use for identifying the model instance.</param>
    /// <param name="autoMap">The auto mapping behavior. Defaults to Enabled.</param>
    internal static void AddSetMappingWithKey(this IDictionary<EventType, FromDefinition> targetFrom, Func<Type, EventType> getOrCreateEventType, INamingPolicy namingPolicy, Type eventType, string propertyName, string eventPropertyName, string? key, AutoMap autoMap = AutoMap.Enabled)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var fromDefinition = targetFrom.GetOrCreateFromDefinition(eventTypeId);
        var eventPropertyPath = new PropertyPath(eventPropertyName);
        fromDefinition.Properties[propertyName] = namingPolicy.GetPropertyName(eventPropertyPath);

        if (!string.IsNullOrEmpty(key))
        {
            var keyPropertyPath = new PropertyPath(key);
            fromDefinition.Key = namingPolicy.GetPropertyName(keyPropertyPath);
        }
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
    /// Adds a context property mapping to the From definition for a given event type.
    /// </summary>
    /// <param name="targetFrom">The target From dictionary to add the mapping to.</param>
    /// <param name="getOrCreateEventType">Function to get or create a cached EventType instance.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="eventType">The event type to map from.</param>
    /// <param name="propertyName">The property name on the projection model.</param>
    /// <param name="contextPropertyName">The property name on the event context.</param>
    internal static void AddContextPropertyMapping(this IDictionary<EventType, FromDefinition> targetFrom, Func<Type, EventType> getOrCreateEventType, INamingPolicy namingPolicy, Type eventType, string propertyName, string contextPropertyName)
    {
        var eventTypeId = getOrCreateEventType(eventType);
        var fromDefinition = targetFrom.GetOrCreateFromDefinition(eventTypeId);
        var contextPropertyPath = new PropertyPath(contextPropertyName);
        var convertedContextPropertyName = namingPolicy.GetPropertyName(contextPropertyPath);
        fromDefinition.Properties[propertyName] = $"{WellKnownExpressions.EventContext}({convertedContextPropertyName})";
    }

    /// <summary>
    /// Auto-maps properties from event type to model type by matching property names.
    /// </summary>
    /// <param name="fromDefinition">The FromDefinition to add property mappings to.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <param name="eventType">The event type to map from.</param>
    /// <param name="modelType">The model type to map to.</param>
    internal static void AutoMapMatchingProperties(this FromDefinition fromDefinition, INamingPolicy namingPolicy, Type eventType, Type modelType)
    {
        foreach (var modelProperty in modelType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            var eventProperty = eventType.GetProperty(modelProperty.Name);
            if (eventProperty is not null)
            {
                var modelPropertyPath = new PropertyPath(modelProperty.Name);
                var modelPropertyName = namingPolicy.GetPropertyName(modelPropertyPath);

                // Skip if the property already has an explicit mapping (including operation mappings)
                if (fromDefinition.Properties.ContainsKey(modelPropertyName))
                {
                    continue;
                }

                var eventPropertyPath = new PropertyPath(eventProperty.Name);
                fromDefinition.Properties[modelPropertyName] = namingPolicy.GetPropertyName(eventPropertyPath);
            }
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
