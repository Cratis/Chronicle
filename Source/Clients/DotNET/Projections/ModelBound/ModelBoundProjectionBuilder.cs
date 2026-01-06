// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
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
    readonly Dictionary<string, EventType> _eventTypeCache = new();
    readonly List<(string PropertyName, FromEveryAttribute Attribute)> _fromEveryAttributes = [];

    /// <summary>
    /// Builds a projection definition from a type with model-bound projection attributes.
    /// </summary>
    /// <param name="modelType">The type of the read model.</param>
    /// <returns>The <see cref="ProjectionDefinition"/>.</returns>
    public ProjectionDefinition Build(Type modelType)
    {
        _fromEveryAttributes.Clear();

        var projectionId = new ProjectionId(modelType.FullName!);
        var readModelIdentifier = modelType.GetReadModelIdentifier();
        var fromEventSequenceAttr = modelType.GetCustomAttribute<FromEventSequenceAttribute>();
        var notRewindableAttr = modelType.GetCustomAttribute<NotRewindableAttribute>();

        var definition = new ProjectionDefinition
        {
            EventSequenceId = fromEventSequenceAttr?.EventSequenceId ?? EventSequenceId.Log,
            Identifier = projectionId,
            ReadModel = readModelIdentifier,
            IsActive = !modelType.IsPassive(),
            IsRewindable = notRewindableAttr is null,
            InitialModelState = "{}",
            From = new Dictionary<EventType, FromDefinition>(),
            Join = new Dictionary<EventType, JoinDefinition>(),
            Children = new Dictionary<string, ChildrenDefinition>(),
            All = new FromEveryDefinition(),
            RemovedWith = new Dictionary<EventType, RemovedWithDefinition>(),
            RemovedWithJoin = new Dictionary<EventType, RemovedWithJoinDefinition>(),
            Tags = modelType.GetTags().ToArray()
        };

        var classLevelFromEvents = modelType.GetCustomAttributes()
            .Where(attr => attr.GetType().IsGenericType &&
                          attr.GetType().GetGenericTypeDefinition() == typeof(FromEventAttribute<>))
            .ToList();

        ProcessClassLevelRemovedWith(modelType, definition);
        var primaryConstructor = ProcessRecord(modelType, definition, classLevelFromEvents);
        ProcessProperties(modelType, definition, classLevelFromEvents, primaryConstructor);
        BuildFromEveryDefinition(definition);

        return definition;
    }

    EventType GetOrCreateEventType(Type eventType)
    {
        var chronicleEventType = _eventTypes.GetEventTypeFor(eventType);
        var key = $"{chronicleEventType.Id}_{chronicleEventType.Generation}_{chronicleEventType.Tombstone}";

        if (!_eventTypeCache.TryGetValue(key, out var cachedEventType))
        {
            cachedEventType = chronicleEventType.ToContract();
            _eventTypeCache[key] = cachedEventType;
        }

        return cachedEventType;
    }

    void BuildFromEveryDefinition(ProjectionDefinition definition)
    {
        if (_fromEveryAttributes.Count == 0)
        {
            return;
        }

        var properties = new Dictionary<string, string>();

        foreach (var (propertyName, attribute) in _fromEveryAttributes)
        {
            if (!string.IsNullOrEmpty(attribute.ContextProperty))
            {
                // Map from event context property
                PropertyValidator.ValidatePropertyExists<EventContext>(attribute.ContextProperty);
                var contextPropertyPath = new PropertyPath(attribute.ContextProperty);
                var convertedContextPropertyName = _namingPolicy.GetPropertyName(contextPropertyPath);
                properties[propertyName] = $"{WellKnownExpressions.EventContext}({convertedContextPropertyName})";
            }
            else if (!string.IsNullOrEmpty(attribute.Property))
            {
                // Map from event property
                var eventPropertyPath = new PropertyPath(attribute.Property);
                properties[propertyName] = _namingPolicy.GetPropertyName(eventPropertyPath);
            }
            else
            {
                // If neither is specified, map from matching property name on event
                properties[propertyName] = propertyName;
            }
        }

        definition.All = new FromEveryDefinition
        {
            Properties = properties,
            IncludeChildren = true
        };
    }

    void ProcessClassLevelRemovedWith(Type modelType, ProjectionDefinition definition)
    {
        foreach (var (attr, eventType) in modelType.GetAttributesOfGenericType<RemovedWithAttribute<object>>())
        {
            definition.RemovedWith.ProcessRemovedWithAttribute(GetOrCreateEventType, attr, eventType);
        }

        foreach (var (attr, eventType) in modelType.GetAttributesOfGenericType<RemovedWithJoinAttribute<object>>())
        {
            definition.RemovedWithJoin.ProcessRemovedWithJoinAttribute(GetOrCreateEventType, attr, eventType);
        }
    }

    void ProcessProperties(Type modelType, ProjectionDefinition definition, List<Attribute> classLevelFromEvents, ConstructorInfo? primaryConstructor)
    {
        foreach (var property in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (primaryConstructor?.GetParameters().Any(p => p.Name?.Equals(property.Name, StringComparison.OrdinalIgnoreCase) == true) == true)
            {
                continue;
            }

            ProcessMember(property, definition, classLevelFromEvents, isRoot: true, modelType: modelType);
        }
    }

    ConstructorInfo? ProcessRecord(Type modelType, ProjectionDefinition definition, List<Attribute> classLevelFromEvents)
    {
        var constructors = modelType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

        if (primaryConstructor is not null)
        {
            var allEventTypesReferencedByModel = new HashSet<Type>();

            foreach (var parameter in primaryConstructor.GetParameters())
            {
                ProcessParameter(parameter, definition, classLevelFromEvents, allEventTypesReferencedByModel, isRoot: true, modelType);
            }
        }

        return primaryConstructor;
    }

    void ProcessParameter(
        ParameterInfo parameter,
        ProjectionDefinition definition,
        List<Attribute> classLevelFromEvents,
        HashSet<Type> allEventTypesReferencedByModel,
        bool isRoot,
        Type? modelType = null,
        ChildrenDefinition? childrenDef = null)
    {
        var memberName = parameter.Name!;
        var propertyPath = new PropertyPath(memberName);
        var propertyName = _namingPolicy.GetPropertyName(propertyPath);

        var targetFrom = isRoot ? definition.From : childrenDef?.From ?? definition.From;
        var targetJoin = isRoot ? definition.Join : childrenDef?.Join ?? definition.Join;
        var targetRemovedWith = isRoot ? definition.RemovedWith : childrenDef?.RemovedWith ?? definition.RemovedWith;
        var targetRemovedWithJoin = isRoot ? definition.RemovedWithJoin : childrenDef?.RemovedWithJoin ?? definition.RemovedWithJoin;

        void TrackAndProcess<TAttribute>(Action<IDictionary<EventType, FromDefinition>, Type, string, string> mappingAction)
            where TAttribute : Attribute
        {
            var attributes = parameter.GetCustomAttributes()
                .Where(attr => attr.GetType().IsGenericType &&
                              attr.GetType().GetGenericTypeDefinition() == typeof(TAttribute).GetGenericTypeDefinition())
                .ToList();

            foreach (var attr in attributes)
            {
                var eventType = attr.GetType().GetGenericArguments()[0];
                allEventTypesReferencedByModel.Add(eventType);
                var eventPropertyNameProperty = attr.GetType().GetProperty(nameof(ICanMapToEventProperty.EventPropertyName));
                var eventPropertyName = eventPropertyNameProperty?.GetValue(attr) as string;
                var propertyToUse = eventPropertyName ?? parameter.Name!;
                PropertyValidator.ValidatePropertyExists(eventType, propertyToUse);
                mappingAction(targetFrom, eventType, propertyName, propertyToUse);
            }
        }

        TrackAndProcess<SetFromAttribute<object>>((from, eventType, prop, eventProp) => from.AddSetMapping(GetOrCreateEventType, _namingPolicy, eventType, prop, eventProp));
        TrackAndProcess<AddFromAttribute<object>>((from, eventType, prop, eventProp) => from.AddAddMapping(GetOrCreateEventType, _namingPolicy, eventType, prop, eventProp));
        TrackAndProcess<SubtractFromAttribute<object>>((from, eventType, prop, eventProp) => from.AddSubtractMapping(GetOrCreateEventType, _namingPolicy, eventType, prop, eventProp));
        TrackAndProcess<IncrementAttribute<object>>((from, eventType, prop, _) => from.AddIncrementMapping(GetOrCreateEventType, eventType, prop));
        TrackAndProcess<DecrementAttribute<object>>((from, eventType, prop, _) => from.AddDecrementMapping(GetOrCreateEventType, eventType, prop));
        TrackAndProcess<CountAttribute<object>>((from, eventType, prop, _) => from.AddCountMapping(GetOrCreateEventType, eventType, prop));

        foreach (var (attr, eventType) in parameter.GetAttributesOfGenericType<SetFromContextAttribute<object>>())
        {
            allEventTypesReferencedByModel.Add(eventType);
            var contextPropertyNameProperty = attr.GetType().GetProperty(nameof(SetFromContextAttribute<object>.ContextPropertyName));
            var contextPropertyName = contextPropertyNameProperty?.GetValue(attr) as string;
            var propertyToValidate = contextPropertyName ?? parameter.Name!;
            PropertyValidator.ValidatePropertyExists<EventContext>(propertyToValidate);
            targetFrom.AddContextPropertyMapping(GetOrCreateEventType, _namingPolicy, eventType, propertyName, propertyToValidate);
        }

        foreach (var (attr, eventType) in parameter.GetAttributesOfGenericType<JoinAttribute<object>>())
        {
            targetJoin.ProcessJoinAttribute(GetOrCreateEventType, _namingPolicy, attr, eventType, memberName, propertyName);
        }

        foreach (var (attr, eventType) in parameter.GetAttributesOfGenericType<RemovedWithAttribute<object>>())
        {
            targetRemovedWith.ProcessRemovedWithAttribute(GetOrCreateEventType, attr, eventType);
        }

        foreach (var (attr, eventType) in parameter.GetAttributesOfGenericType<RemovedWithJoinAttribute<object>>())
        {
            targetRemovedWithJoin.ProcessRemovedWithJoinAttribute(GetOrCreateEventType, attr, eventType);
        }

        // Only process ChildrenFromAttribute when at root level.
        // For nested children, ProcessNestedChildren handles adding them to the correct parent ChildrenDefinition.
        if (isRoot)
        {
            foreach (var (attr, eventType) in parameter.GetAttributesOfGenericType<ChildrenFromAttribute<object>>())
            {
                definition.ProcessChildrenFromAttribute(GetOrCreateEventType, _namingPolicy, memberName, parameter.ParameterType, attr, eventType, ProcessMember, modelType);
            }
        }

        foreach (var attr in classLevelFromEvents)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            var eventProperty = eventType.GetProperty(memberName);

            if (eventProperty is not null)
            {
                allEventTypesReferencedByModel.Add(eventType);
                var keyProperty = attr.GetType().GetProperty(nameof(FromEventAttribute<object>.Key));
                var key = keyProperty?.GetValue(attr) as string;
                if (!string.IsNullOrEmpty(key))
                {
                    PropertyValidator.ValidatePropertyExists(eventType, key);
                }
                targetFrom.AddSetMappingWithKey(GetOrCreateEventType, _namingPolicy, eventType, propertyName, memberName, key);
            }
        }

        var fromEveryAttr = parameter.GetCustomAttribute<FromEveryAttribute>();
        if (fromEveryAttr is not null)
        {
            _fromEveryAttributes.Add((propertyName, fromEveryAttr));
        }
    }

    void ProcessMember(
        MemberInfo property,
        ProjectionDefinition definition,
        List<Attribute> classLevelFromEvents,
        bool isRoot,
        Type? modelType = null,
        ChildrenDefinition? childrenDef = null)
    {
        var propertyPath = new PropertyPath(property.Name);
        var propertyName = _namingPolicy.GetPropertyName(propertyPath);

        var targetFrom = isRoot ? definition.From : childrenDef?.From ?? definition.From;
        var targetJoin = isRoot ? definition.Join : childrenDef?.Join ?? definition.Join;
        var targetRemovedWith = isRoot ? definition.RemovedWith : childrenDef?.RemovedWith ?? definition.RemovedWith;
        var targetRemovedWithJoin = isRoot ? definition.RemovedWithJoin : childrenDef?.RemovedWithJoin ?? definition.RemovedWithJoin;

        var eventTypesReferencedByMember = new HashSet<Type>();

        void TrackAndProcess<TAttribute>(Action<IDictionary<EventType, FromDefinition>, Type, string, string> mappingAction)
            where TAttribute : Attribute
        {
            var attributes = property.GetCustomAttributes()
                .Where(attr => attr.GetType().IsGenericType &&
                              attr.GetType().GetGenericTypeDefinition() == typeof(TAttribute).GetGenericTypeDefinition())
                .ToList();

            foreach (var attr in attributes)
            {
                var eventType = attr.GetType().GetGenericArguments()[0];
                eventTypesReferencedByMember.Add(eventType);
                var eventPropertyNameProperty = attr.GetType().GetProperty(nameof(ICanMapToEventProperty.EventPropertyName));
                var eventPropertyName = eventPropertyNameProperty?.GetValue(attr) as string;
                var propertyToUse = eventPropertyName ?? property.Name;
                PropertyValidator.ValidatePropertyExists(eventType, propertyToUse);
                mappingAction(targetFrom, eventType, propertyName, propertyToUse);
            }
        }

        TrackAndProcess<SetFromAttribute<object>>((from, eventType, prop, eventProp) => from.AddSetMapping(GetOrCreateEventType, _namingPolicy, eventType, prop, eventProp));
        TrackAndProcess<AddFromAttribute<object>>((from, eventType, prop, eventProp) => from.AddAddMapping(GetOrCreateEventType, _namingPolicy, eventType, prop, eventProp));
        TrackAndProcess<SubtractFromAttribute<object>>((from, eventType, prop, eventProp) => from.AddSubtractMapping(GetOrCreateEventType, _namingPolicy, eventType, prop, eventProp));
        TrackAndProcess<IncrementAttribute<object>>((from, eventType, prop, _) => from.AddIncrementMapping(GetOrCreateEventType, eventType, prop));
        TrackAndProcess<DecrementAttribute<object>>((from, eventType, prop, _) => from.AddDecrementMapping(GetOrCreateEventType, eventType, prop));
        TrackAndProcess<CountAttribute<object>>((from, eventType, prop, _) => from.AddCountMapping(GetOrCreateEventType, eventType, prop));

        foreach (var (attr, eventType) in property.GetAttributesOfGenericType<SetFromContextAttribute<object>>())
        {
            eventTypesReferencedByMember.Add(eventType);
            var contextPropertyNameProperty = attr.GetType().GetProperty(nameof(SetFromContextAttribute<object>.ContextPropertyName));
            var contextPropertyName = contextPropertyNameProperty?.GetValue(attr) as string;
            var propertyToValidate = contextPropertyName ?? property.Name;
            PropertyValidator.ValidatePropertyExists<EventContext>(propertyToValidate);
            targetFrom.AddContextPropertyMapping(GetOrCreateEventType, _namingPolicy, eventType, propertyName, propertyToValidate);
        }

        foreach (var (attr, eventType) in property.GetAttributesOfGenericType<JoinAttribute<object>>())
        {
            targetJoin.ProcessJoinAttribute(GetOrCreateEventType, _namingPolicy, attr, eventType, property.Name, propertyName);
        }

        foreach (var (attr, eventType) in property.GetAttributesOfGenericType<RemovedWithAttribute<object>>())
        {
            targetRemovedWith.ProcessRemovedWithAttribute(GetOrCreateEventType, attr, eventType);
        }

        foreach (var (attr, eventType) in property.GetAttributesOfGenericType<RemovedWithJoinAttribute<object>>())
        {
            targetRemovedWithJoin.ProcessRemovedWithJoinAttribute(GetOrCreateEventType, attr, eventType);
        }

        // Only process ChildrenFromAttribute when at root level.
        // For nested children, ProcessNestedChildren handles adding them to the correct parent ChildrenDefinition.
        if (isRoot)
        {
            foreach (var (attr, eventType) in property.GetAttributesOfGenericType<ChildrenFromAttribute<object>>())
            {
                var memberType = property is PropertyInfo propInfo ? propInfo.PropertyType : throw new InvalidOperationException("Expected PropertyInfo");
                definition.ProcessChildrenFromAttribute(GetOrCreateEventType, _namingPolicy, property.Name, memberType, attr, eventType, ProcessMember, modelType);
            }
        }

        foreach (var attr in classLevelFromEvents)
        {
            var eventType = attr.GetType().GetGenericArguments()[0];
            var eventProperty = eventType.GetProperty(property.Name);

            if (eventProperty is not null)
            {
                eventTypesReferencedByMember.Add(eventType);
                var keyProperty = attr.GetType().GetProperty(nameof(FromEventAttribute<object>.Key));
                var key = keyProperty?.GetValue(attr) as string;
                if (!string.IsNullOrEmpty(key))
                {
                    PropertyValidator.ValidatePropertyExists(eventType, key);
                }
                targetFrom.AddSetMappingWithKey(GetOrCreateEventType, _namingPolicy, eventType, propertyName, property.Name, key);
            }
        }

        var fromEveryAttr = property.GetCustomAttribute<FromEveryAttribute>();
        if (fromEveryAttr is not null)
        {
            _fromEveryAttributes.Add((propertyName, fromEveryAttr));
        }
    }
}
