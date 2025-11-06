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

        var primaryConstructor = ProcessRecord(modelType, definition, classLevelFromEvents);
        ProcessProperties(modelType, definition, classLevelFromEvents, primaryConstructor);

        return definition;
    }

    void ProcessProperties(Type modelType, ProjectionDefinition definition, List<Attribute> classLevelFromEvents, ConstructorInfo? primaryConstructor)
    {
        foreach (var property in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (primaryConstructor?.GetParameters().Any(p => p.Name?.Equals(property.Name, StringComparison.OrdinalIgnoreCase) == true) == true)
            {
                continue;
            }

            ProcessMember(property, definition, classLevelFromEvents, isRoot: true);
        }
    }

    ConstructorInfo? ProcessRecord(Type modelType, ProjectionDefinition definition, List<Attribute> classLevelFromEvents)
    {
        var constructors = modelType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

        if (primaryConstructor is not null)
        {
            foreach (var parameter in primaryConstructor.GetParameters())
            {
                ProcessParameter(parameter, definition, classLevelFromEvents, isRoot: true);
            }
        }

        return primaryConstructor;
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

        parameter.ProcessParameterAttributesByType<SetFromAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, eventProp) => from.AddSetMapping(_eventTypes, _namingPolicy, eventType, prop, eventProp));
        parameter.ProcessParameterAttributesByType<AddFromAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, eventProp) => from.AddAddMapping(_eventTypes, _namingPolicy, eventType, prop, eventProp));
        parameter.ProcessParameterAttributesByType<SubtractFromAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, eventProp) => from.AddSubtractMapping(_eventTypes, _namingPolicy, eventType, prop, eventProp));
        parameter.ProcessParameterAttributesByType<IncrementAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, _) => from.AddIncrementMapping(_eventTypes, eventType, prop));
        parameter.ProcessParameterAttributesByType<DecrementAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, _) => from.AddDecrementMapping(_eventTypes, eventType, prop));
        parameter.ProcessParameterAttributesByType<CountAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, _) => from.AddCountMapping(_eventTypes, eventType, prop));

        foreach (var (attr, eventType) in parameter.GetAttributesOfGenericType<JoinAttribute<object>>())
        {
            targetJoin.ProcessJoinAttribute(_eventTypes, _namingPolicy, attr, eventType, memberName, propertyName);
        }

        foreach (var (attr, eventType) in parameter.GetAttributesOfGenericType<RemovedWithAttribute<object>>())
        {
            targetRemovedWith.ProcessRemovedWithAttribute(_eventTypes, attr, eventType);
        }

        foreach (var (attr, eventType) in parameter.GetAttributesOfGenericType<RemovedWithJoinAttribute<object>>())
        {
            targetRemovedWithJoin.ProcessRemovedWithJoinAttribute(_eventTypes, attr, eventType);
        }

        foreach (var (attr, eventType) in parameter.GetAttributesOfGenericType<ChildrenFromAttribute<object>>())
        {
            definition.ProcessChildrenFromAttribute(_eventTypes, _namingPolicy, memberName, parameter.ParameterType, attr, eventType, ProcessMember);
        }

        foreach (var (eventType, eventProperty) in classLevelFromEvents
            .Select(attr => (
            eventType: attr.GetType().GetGenericArguments()[0],
            eventProperty: attr.GetType().GetGenericArguments()[0].GetProperty(memberName)))
            .Where(x => x.eventProperty is not null))
        {
            targetFrom.AddSetMapping(_eventTypes, _namingPolicy, eventType, propertyName, memberName);
        }
    }

    void ProcessMember(
        MemberInfo property,
        ProjectionDefinition definition,
        List<Attribute> classLevelFromEvents,
        bool isRoot,
        ChildrenDefinition? childrenDef = null)
    {
        var propertyPath = new PropertyPath(property.Name);
        var propertyName = _namingPolicy.GetPropertyName(propertyPath);

        var targetFrom = isRoot ? definition.From : childrenDef?.From ?? definition.From;
        var targetJoin = isRoot ? definition.Join : childrenDef?.Join ?? definition.Join;
        var targetRemovedWith = isRoot ? definition.RemovedWith : childrenDef?.RemovedWith ?? definition.RemovedWith;
        var targetRemovedWithJoin = isRoot ? definition.RemovedWithJoin : childrenDef?.RemovedWithJoin ?? definition.RemovedWithJoin;

        property.ProcessAttributesByType<SetFromAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, eventProp) => from.AddSetMapping(_eventTypes, _namingPolicy, eventType, prop, eventProp));
        property.ProcessAttributesByType<AddFromAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, eventProp) => from.AddAddMapping(_eventTypes, _namingPolicy, eventType, prop, eventProp));
        property.ProcessAttributesByType<SubtractFromAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, eventProp) => from.AddSubtractMapping(_eventTypes, _namingPolicy, eventType, prop, eventProp));
        property.ProcessAttributesByType<IncrementAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, _) => from.AddIncrementMapping(_eventTypes, eventType, prop));
        property.ProcessAttributesByType<DecrementAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, _) => from.AddDecrementMapping(_eventTypes, eventType, prop));
        property.ProcessAttributesByType<CountAttribute<object>>(propertyName, targetFrom, (from, eventType, prop, _) => from.AddCountMapping(_eventTypes, eventType, prop));

        foreach (var (attr, eventType) in property.GetAttributesOfGenericType<JoinAttribute<object>>())
        {
            targetJoin.ProcessJoinAttribute(_eventTypes, _namingPolicy, attr, eventType, property.Name, propertyName);
        }

        foreach (var (attr, eventType) in property.GetAttributesOfGenericType<RemovedWithAttribute<object>>())
        {
            targetRemovedWith.ProcessRemovedWithAttribute(_eventTypes, attr, eventType);
        }

        foreach (var (attr, eventType) in property.GetAttributesOfGenericType<RemovedWithJoinAttribute<object>>())
        {
            targetRemovedWithJoin.ProcessRemovedWithJoinAttribute(_eventTypes, attr, eventType);
        }

        foreach (var (attr, eventType) in property.GetAttributesOfGenericType<ChildrenFromAttribute<object>>())
        {
            var memberType = property is PropertyInfo propInfo ? propInfo.PropertyType : throw new InvalidOperationException("Expected PropertyInfo");
            definition.ProcessChildrenFromAttribute(_eventTypes, _namingPolicy, property.Name, memberType, attr, eventType, ProcessMember);
        }

        foreach (var (eventType, eventProperty) in classLevelFromEvents
            .Select(attr => (
                eventType: attr.GetType().GetGenericArguments()[0],
                eventProperty: attr.GetType().GetGenericArguments()[0].GetProperty(property.Name)))
            .Where(x => x.eventProperty is not null))
        {
            targetFrom.AddSetMapping(_eventTypes, _namingPolicy, eventType, propertyName, property.Name);
        }
    }
}
