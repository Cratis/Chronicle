// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.Sinks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Sinks;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Builds projection definitions from model-bound attributes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ModelBoundProjectionBuilder"/> class.
/// </remarks>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
public class ModelBoundProjectionBuilder(
    INamingPolicy namingPolicy,
    IEventTypes eventTypes,
    JsonSerializerOptions jsonSerializerOptions)
{
    readonly INamingPolicy _namingPolicy = namingPolicy;
    readonly IEventTypes _eventTypes = eventTypes;

    /// <summary>
    /// Builds a projection definition from a type decorated with <see cref="ReadModelAttribute"/>.
    /// </summary>
    /// <param name="modelType">The type of the read model.</param>
    /// <returns>The <see cref="ProjectionDefinition"/> or null if the type is not decorated with <see cref="ReadModelAttribute"/>.</returns>
    public ProjectionDefinition? Build(Type modelType)
    {
        var readModelAttribute = modelType.GetCustomAttribute<ReadModelAttribute>();
        if (readModelAttribute is null)
        {
            return null;
        }

        var projectionId = !string.IsNullOrEmpty(readModelAttribute.Id)
            ? new ProjectionId(readModelAttribute.Id)
            : new ProjectionId(modelType.FullName!);

        var readModelIdentifier = modelType.GetReadModelIdentifier();

        var definition = new ProjectionDefinition
        {
            EventSequenceId = readModelAttribute.EventSequenceId,
            Identifier = projectionId,
            ReadModel = readModelIdentifier,
            IsActive = true,
            IsRewindable = true,
            InitialModelState = "{}",
            From = new Dictionary<EventType, FromDefinition>(),
            Join = new Dictionary<EventType, JoinDefinition>(),
            Children = new Dictionary<string, ChildrenDefinition>(),
            All = default!,
            RemovedWith = new Dictionary<EventType, RemovedWithDefinition>(),
            RemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>(),
            Sink = new SinkDefinition
            {
                ConfigurationId = Guid.Empty,
                TypeId = WellKnownSinkTypes.MongoDB
            }
        };

        // Process class-level FromEvent attributes
        var classLevelFromEvents = modelType.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(FromEventAttribute<>))
            .ToList();

        // Get all properties
        var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Find the key property
        var keyProperty = properties.FirstOrDefault(p => p.GetCustomAttribute<ModelKeyAttribute>() is not null);

        // Process properties
        foreach (var property in properties)
        {
            ProcessProperty(property, definition, classLevelFromEvents);
        }

        return definition;
    }

    void ProcessProperty(
        PropertyInfo property,
        ProjectionDefinition definition,
        List<Attribute> classLevelFromEvents)
    {
        var propertyPath = new PropertyPath(property.Name);
        var propertyName = _namingPolicy.GetPropertyName(propertyPath);

        // Check for FromEventSourceId attribute
        var fromEventSourceId = property.GetCustomAttribute<FromEventSourceIdAttribute>();

        // Check for SetFrom attributes
        var setFromAttributes = property.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(SetFromAttribute<>))
            .ToList();

        // Check for AddFrom attributes
        var addFromAttributes = property.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(AddFromAttribute<>))
            .ToList();

        // Check for SubtractFrom attributes
        var subtractFromAttributes = property.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(SubtractFromAttribute<>))
            .ToList();

        // Check for ChildrenFrom attributes
        var childrenFromAttributes = property.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(ChildrenFromAttribute<>))
            .ToList();

        // Process SetFrom attributes
        foreach (var attr in setFromAttributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            var eventPropertyNameProperty = attr.GetType().GetProperty(nameof(SetFromAttribute<object>.EventPropertyName));
            var eventPropertyName = eventPropertyNameProperty?.GetValue(attr) as string;

            AddSetMapping(definition, eventType, propertyName, eventPropertyName ?? property.Name);
        }

        // Process AddFrom attributes
        foreach (var attr in addFromAttributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            var eventPropertyNameProperty = attr.GetType().GetProperty(nameof(AddFromAttribute<object>.EventPropertyName));
            var eventPropertyName = eventPropertyNameProperty?.GetValue(attr) as string;

            AddAddMapping(definition, eventType, propertyName, eventPropertyName ?? property.Name);
        }

        // Process SubtractFrom attributes
        foreach (var attr in subtractFromAttributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            var eventPropertyNameProperty = attr.GetType().GetProperty(nameof(SubtractFromAttribute<object>.EventPropertyName));
            var eventPropertyName = eventPropertyNameProperty?.GetValue(attr) as string;

            AddSubtractMapping(definition, eventType, propertyName, eventPropertyName ?? property.Name);
        }

        // Process ChildrenFrom attributes
        foreach (var attr in childrenFromAttributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            ProcessChildrenFrom(definition, property, attr, eventType);
        }

        // Process class-level FromEvent attributes
        foreach (var attr in classLevelFromEvents)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];

            // Check if event has a property with matching name
            var eventProperty = eventType.GetProperty(property.Name);
            if (eventProperty is not null)
            {
                AddSetMapping(definition, eventType, propertyName, property.Name);
            }
        }

        // Handle FromEventSourceId
        if (fromEventSourceId is not null)
        {
            // This is handled at the From/Join level through key configuration
            // We don't need to do anything here as the key is set at the builder level
        }
    }

    void AddSetMapping(ProjectionDefinition definition, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();

        if (!definition.From.TryGetValue(eventTypeId, out var value))
        {
            value = new FromDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            definition.From[eventTypeId] = value;
        }

        var eventPropertyPath = new PropertyPath(eventPropertyName);
        value.Properties[propertyName] = _namingPolicy.GetPropertyName(eventPropertyPath);
    }

    void AddAddMapping(ProjectionDefinition definition, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();

        if (!definition.From.TryGetValue(eventTypeId, out var value))
        {
            value = new FromDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            definition.From[eventTypeId] = value;
        }

        var eventPropertyPath = new PropertyPath(eventPropertyName);
        var convertedEventPropertyName = _namingPolicy.GetPropertyName(eventPropertyPath);
        value.Properties[propertyName] = $"{WellKnownExpressions.Add}({convertedEventPropertyName})";
    }

    void AddSubtractMapping(ProjectionDefinition definition, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();

        if (!definition.From.TryGetValue(eventTypeId, out var value))
        {
            value = new FromDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            definition.From[eventTypeId] = value;
        }

        var eventPropertyPath = new PropertyPath(eventPropertyName);
        var convertedEventPropertyName = _namingPolicy.GetPropertyName(eventPropertyPath);
        value.Properties[propertyName] = $"{WellKnownExpressions.Subtract}({convertedEventPropertyName})";
    }

    void ProcessChildrenFrom(ProjectionDefinition definition, PropertyInfo property, Attribute attr, Type eventType)
    {
        var propertyPath = new PropertyPath(property.Name);
        var propertyName = _namingPolicy.GetPropertyName(propertyPath);
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();

        var keyProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.Key));
        var identifiedByProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.IdentifiedBy));
        var parentKeyProperty = attr.GetType().GetProperty(nameof(ChildrenFromAttribute<object>.ParentKey));

        var key = keyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;
        var identifiedBy = identifiedByProperty?.GetValue(attr) as string ?? WellKnownExpressions.Id;
        var parentKey = parentKeyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;

        if (!definition.Children.TryGetValue(propertyName, out var childrenDef))
        {
            childrenDef = new ChildrenDefinition
            {
                IdentifiedBy = identifiedBy,
                From = new Dictionary<EventType, FromDefinition>(),
                Join = new Dictionary<EventType, JoinDefinition>(),
                RemovedWith = new Dictionary<EventType, RemovedWithDefinition>(),
                RemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>()
            };
            definition.Children[propertyName] = childrenDef;
        }

        // Convert key to proper expression
        var keyExpression = key == WellKnownExpressions.EventSourceId ? key : _namingPolicy.GetPropertyName(new PropertyPath(key));

        childrenDef.From[eventTypeId] = new FromDefinition
        {
            Key = keyExpression,
            ParentKey = parentKey,
            Properties = new Dictionary<string, string>()
        };
    }
}
