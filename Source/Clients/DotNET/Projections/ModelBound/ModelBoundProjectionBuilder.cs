// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Contracts.Sinks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
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
internal class ModelBoundProjectionBuilder(
    INamingPolicy namingPolicy,
    IEventTypes eventTypes) : IModelBoundProjectionBuilder
{
    readonly INamingPolicy _namingPolicy = namingPolicy;
    readonly IEventTypes _eventTypes = eventTypes;

    /// <summary>
    /// Builds a projection definition from a type with model-bound projection attributes.
    /// </summary>
    /// <param name="modelType">The type of the read model.</param>
    /// <returns>The <see cref="ProjectionDefinition"/>.</returns>
    public ProjectionDefinition Build(Type modelType)
    {
        var projectionId = new ProjectionId(modelType.FullName!);
        var readModelIdentifier = modelType.GetReadModelIdentifier();
        var fromEventSequenceAttr = modelType.GetCustomAttribute<FromEventSequenceAttribute>();
        var notRewindableAttr = modelType.GetCustomAttribute<NotRewindableAttribute>();
        var passiveAttr = modelType.GetCustomAttribute<PassiveAttribute>();

        var definition = new ProjectionDefinition
        {
            EventSequenceId = fromEventSequenceAttr?.EventSequenceId ?? EventSequenceId.Log,
            Identifier = projectionId,
            ReadModel = readModelIdentifier,
            IsActive = passiveAttr is null,
            IsRewindable = notRewindableAttr is null,
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

        var classLevelFromEvents = modelType.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(FromEventAttribute<>))
            .ToList();

        // Process constructor parameters (for records with primary constructors)
        var constructors = modelType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

        if (primaryConstructor is not null)
        {
            foreach (var parameter in primaryConstructor.GetParameters())
            {
                ProcessParameter(parameter, definition, classLevelFromEvents, isRoot: true);
            }
        }

        // Process properties (for backwards compatibility and non-record types)
        foreach (var property in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Skip properties that were already processed as constructor parameters
            if (primaryConstructor?.GetParameters().Any(p => p.Name?.Equals(property.Name, StringComparison.OrdinalIgnoreCase) == true) == true)
            {
                continue;
            }

            ProcessMember(property, definition, classLevelFromEvents, isRoot: true);
        }

        return definition;
    }

    void ProcessParameter(
        ParameterInfo parameter,
        ProjectionDefinition definition,
        List<Attribute> classLevelFromEvents,
        bool isRoot,
        ChildrenDefinition? childrenDef = null)
    {
        var memberName = parameter.Name!;
        var propertyPath = new PropertyPath(memberName);
        var propertyName = _namingPolicy.GetPropertyName(propertyPath);

        var targetFrom = isRoot ? definition.From : childrenDef?.From ?? definition.From;
        var targetJoin = isRoot ? definition.Join : childrenDef?.Join ?? definition.Join;
        var targetRemovedWith = isRoot ? definition.RemovedWith : childrenDef?.RemovedWith ?? definition.RemovedWith;
        var targetRemovedWithJoin = isRoot ? definition.RemovedWithJoin : childrenDef?.RemovedWithJoin ?? definition.RemovedWithJoin;

        // Process all attribute types
        ProcessParameterAttributesByType<SetFromAttribute<object>>(parameter, propertyName, targetFrom, AddSetMapping);
        ProcessParameterAttributesByType<AddFromAttribute<object>>(parameter, propertyName, targetFrom, AddAddMapping);
        ProcessParameterAttributesByType<SubtractFromAttribute<object>>(parameter, propertyName, targetFrom, AddSubtractMapping);
        ProcessParameterAttributesByType<IncrementAttribute<object>>(parameter, propertyName, targetFrom, AddIncrementMapping);
        ProcessParameterAttributesByType<DecrementAttribute<object>>(parameter, propertyName, targetFrom, AddDecrementMapping);
        ProcessParameterAttributesByType<CountAttribute<object>>(parameter, propertyName, targetFrom, AddCountMapping);

        // Process Join attributes
        var joinAttributes = parameter.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(JoinAttribute<>))
            .ToList();

        foreach (var attr in joinAttributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            ProcessJoinAttributeForParameter(attr, eventType, parameter, propertyName, targetJoin);
        }

        // Process RemovedWith attributes
        var removedWithAttributes = parameter.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(RemovedWithAttribute<>))
            .ToList();

        foreach (var attr in removedWithAttributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            ProcessRemovedWithAttribute(attr, eventType, targetRemovedWith);
        }

        // Process RemovedWithJoin attributes
        var removedWithJoinAttributes = parameter.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(RemovedWithJoinAttribute<>))
            .ToList();

        foreach (var attr in removedWithJoinAttributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            ProcessRemovedWithJoinAttribute(attr, eventType, targetRemovedWithJoin);
        }

        // Process ChildrenFrom attributes
        var childrenFromAttributes = parameter.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(ChildrenFromAttribute<>))
            .ToList();

        foreach (var attr in childrenFromAttributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            ProcessChildrenFromForParameter(definition, parameter, attr, eventType);
        }

        // Process class-level FromEvent attributes
        foreach (var attr in classLevelFromEvents)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];

            // Check if event has a property with matching name
            var eventProperty = eventType.GetProperty(memberName);
            if (eventProperty is not null)
            {
                AddSetMapping(targetFrom, eventType, propertyName, memberName);
            }
        }
    }

    void ProcessMember(
        MemberInfo property,
        ProjectionDefinition definition,
        List<Attribute> classLevelFromEvents,
        bool isRoot,
        ChildrenDefinition? childrenDef = null)
    {
        if (property.Name == "Balance")
        {
            Console.WriteLine("Debug");
        }

        var propertyPath = new PropertyPath(property.Name);
        var propertyName = _namingPolicy.GetPropertyName(propertyPath);

        var targetFrom = isRoot ? definition.From : childrenDef?.From ?? definition.From;
        var targetJoin = isRoot ? definition.Join : childrenDef?.Join ?? definition.Join;
        var targetRemovedWith = isRoot ? definition.RemovedWith : childrenDef?.RemovedWith ?? definition.RemovedWith;
        var targetRemovedWithJoin = isRoot ? definition.RemovedWithJoin : childrenDef?.RemovedWithJoin ?? definition.RemovedWithJoin;

        // Process all attribute types
        ProcessAttributesByType<SetFromAttribute<object>>(property, propertyName, targetFrom, AddSetMapping);
        ProcessAttributesByType<AddFromAttribute<object>>(property, propertyName, targetFrom, AddAddMapping);
        ProcessAttributesByType<SubtractFromAttribute<object>>(property, propertyName, targetFrom, AddSubtractMapping);
        ProcessAttributesByType<IncrementAttribute<object>>(property, propertyName, targetFrom, AddIncrementMapping);
        ProcessAttributesByType<DecrementAttribute<object>>(property, propertyName, targetFrom, AddDecrementMapping);
        ProcessAttributesByType<CountAttribute<object>>(property, propertyName, targetFrom, AddCountMapping);

        // Process Join attributes
        var joinAttributes = property.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(JoinAttribute<>))
            .ToList();

        foreach (var attr in joinAttributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            ProcessJoinAttribute(attr, eventType, property, propertyName, targetJoin);
        }

        // Process RemovedWith attributes
        var removedWithAttributes = property.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(RemovedWithAttribute<>))
            .ToList();

        foreach (var attr in removedWithAttributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            ProcessRemovedWithAttribute(attr, eventType, targetRemovedWith);
        }

        // Process RemovedWithJoin attributes
        var removedWithJoinAttributes = property.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(RemovedWithJoinAttribute<>))
            .ToList();

        foreach (var attr in removedWithJoinAttributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            ProcessRemovedWithJoinAttribute(attr, eventType, targetRemovedWithJoin);
        }

        // Process ChildrenFrom attributes
        var childrenFromAttributes = property.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(ChildrenFromAttribute<>))
            .ToList();

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
                AddSetMapping(targetFrom, eventType, propertyName, property.Name);
            }
        }
    }

    void ProcessAttributesByType<TAttribute>(
        MemberInfo property,
        string propertyName,
        IDictionary<EventType, FromDefinition> targetFrom,
        Action<IDictionary<EventType, FromDefinition>, Type, string, string> mappingAction)
    {
        var attributes = property.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(TAttribute).GetGenericTypeDefinition())
            .ToList();

        foreach (var attr in attributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            var eventPropertyNameProperty = attr.GetType().GetProperty("EventPropertyName");
            var eventPropertyName = eventPropertyNameProperty?.GetValue(attr) as string;

            mappingAction(targetFrom, eventType, propertyName, eventPropertyName ?? property.Name);
        }
    }

    void ProcessParameterAttributesByType<TAttribute>(
        ParameterInfo parameter,
        string propertyName,
        IDictionary<EventType, FromDefinition> targetFrom,
        Action<IDictionary<EventType, FromDefinition>, Type, string, string> mappingAction)
    {
        var attributes = parameter.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(TAttribute).GetGenericTypeDefinition())
            .ToList();

        foreach (var attr in attributes)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            var eventPropertyNameProperty = attr.GetType().GetProperty("EventPropertyName");
            var eventPropertyName = eventPropertyNameProperty?.GetValue(attr) as string;

            mappingAction(targetFrom, eventType, propertyName, eventPropertyName ?? parameter.Name!);
        }
    }

    void ProcessJoinAttribute(Attribute attr, Type eventType, MemberInfo property, string propertyName, IDictionary<EventType, JoinDefinition> targetJoin)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();
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

        var eventPropPath = new PropertyPath(eventPropertyName ?? property.Name);
        joinDef.Properties[propertyName] = _namingPolicy.GetPropertyName(eventPropPath);
    }

    void ProcessJoinAttributeForParameter(Attribute attr, Type eventType, ParameterInfo parameter, string propertyName, IDictionary<EventType, JoinDefinition> targetJoin)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();
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

        var eventPropPath = new PropertyPath(eventPropertyName ?? parameter.Name!);
        joinDef.Properties[propertyName] = _namingPolicy.GetPropertyName(eventPropPath);
    }

    void ProcessRemovedWithAttribute(Attribute attr, Type eventType, IDictionary<EventType, RemovedWithDefinition> targetRemovedWith)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();
        var keyProperty = attr.GetType().GetProperty(nameof(RemovedWithAttribute<object>.Key));
        var parentKeyProperty = attr.GetType().GetProperty(nameof(RemovedWithAttribute<object>.ParentKey));

        var key = keyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;
        var parentKey = parentKeyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;

        targetRemovedWith[eventTypeId] = new RemovedWithDefinition
        {
            Key = key,
            ParentKey = parentKey
        };
    }

    void ProcessRemovedWithJoinAttribute(Attribute attr, Type eventType, IDictionary<EventType, RemovedWithJoinDefinition> targetRemovedWithJoin)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();
        var keyProperty = attr.GetType().GetProperty(nameof(RemovedWithJoinAttribute<object>.Key));

        var key = keyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;

        targetRemovedWithJoin[eventTypeId] = new RemovedWithJoinDefinition
        {
            Key = key
        };
    }

    void AddSetMapping(IDictionary<EventType, FromDefinition> targetFrom, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();

        if (!targetFrom.TryGetValue(eventTypeId, out var value))
        {
            value = new FromDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            targetFrom[eventTypeId] = value;
        }

        var eventPropertyPath = new PropertyPath(eventPropertyName);
        value.Properties[propertyName] = _namingPolicy.GetPropertyName(eventPropertyPath);
    }

    void AddAddMapping(IDictionary<EventType, FromDefinition> targetFrom, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();

        if (!targetFrom.TryGetValue(eventTypeId, out var value))
        {
            value = new FromDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            targetFrom[eventTypeId] = value;
        }

        var eventPropertyPath = new PropertyPath(eventPropertyName);
        var convertedEventPropertyName = _namingPolicy.GetPropertyName(eventPropertyPath);
        value.Properties[propertyName] = $"{WellKnownExpressions.Add}({convertedEventPropertyName})";
    }

    void AddSubtractMapping(IDictionary<EventType, FromDefinition> targetFrom, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();

        if (!targetFrom.TryGetValue(eventTypeId, out var value))
        {
            value = new FromDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            targetFrom[eventTypeId] = value;
        }

        var eventPropertyPath = new PropertyPath(eventPropertyName);
        var convertedEventPropertyName = _namingPolicy.GetPropertyName(eventPropertyPath);
        value.Properties[propertyName] = $"{WellKnownExpressions.Subtract}({convertedEventPropertyName})";
    }

    void AddIncrementMapping(IDictionary<EventType, FromDefinition> targetFrom, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();

        if (!targetFrom.TryGetValue(eventTypeId, out var value))
        {
            value = new FromDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            targetFrom[eventTypeId] = value;
        }

        value.Properties[propertyName] = $"{WellKnownExpressions.Increment}()";
    }

    void AddDecrementMapping(IDictionary<EventType, FromDefinition> targetFrom, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();

        if (!targetFrom.TryGetValue(eventTypeId, out var value))
        {
            value = new FromDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            targetFrom[eventTypeId] = value;
        }

        value.Properties[propertyName] = $"{WellKnownExpressions.Decrement}()";
    }

    void AddCountMapping(IDictionary<EventType, FromDefinition> targetFrom, Type eventType, string propertyName, string eventPropertyName)
    {
        var eventTypeId = _eventTypes.GetEventTypeFor(eventType).ToContract();

        if (!targetFrom.TryGetValue(eventTypeId, out var value))
        {
            value = new FromDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                Properties = new Dictionary<string, string>()
            };
            targetFrom[eventTypeId] = value;
        }

        value.Properties[propertyName] = $"{WellKnownExpressions.Count}()";
    }

    void ProcessChildrenFrom(ProjectionDefinition definition, MemberInfo property, Attribute attr, Type eventType)
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

        // Process attributes on the child type recursively
        var childType = property is PropertyInfo propInfo ? GetChildType(propInfo.PropertyType) : null;
        if (childType is not null)
        {
            foreach (var childProperty in childType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                ProcessMember(childProperty, definition, [], isRoot: false, childrenDef: childrenDef);
            }
        }
    }

    void ProcessChildrenFromForParameter(ProjectionDefinition definition, ParameterInfo parameter, Attribute attr, Type eventType)
    {
        var propertyPath = new PropertyPath(parameter.Name!);
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

        // Process attributes on the child type recursively
        var childType = GetChildType(parameter.ParameterType);
        if (childType is not null)
        {
            foreach (var childProperty in childType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                ProcessMember(childProperty, definition, [], isRoot: false, childrenDef: childrenDef);
            }
        }
    }

    Type? GetChildType(Type propertyType)
    {
        // Check if it's IEnumerable<T>
        if (propertyType.IsGenericType)
        {
            var genericDef = propertyType.GetGenericTypeDefinition();
            if (genericDef == typeof(IEnumerable<>) || genericDef.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                return propertyType.GetGenericArguments()[0];
            }
        }

        // Check interfaces for IEnumerable<T>
        var enumerableInterface = propertyType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerableInterface?.GetGenericArguments()[0];
    }
}
