// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Expressions;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Projections.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using NJsonSchema;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionFactory"/> class.
/// </remarks>
/// <param name="propertyMapperExpressionResolvers"><see cref="IReadModelPropertyExpressionResolvers"/> for resolving expressions for properties.</param>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving expressions for accessing values on events.</param>
/// <param name="keyExpressionResolvers"><see cref="IKeyExpressionResolvers"/> for resolving keys.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting to and from expando objects.</param>
/// <param name="keyResolvers"><see cref="IKeyResolvers"/> for resolving <see cref="Key"/>.</param>
/// <param name="storage"><see cref="IEventStoreNamespaceStorage"/> for accessing underlying storage for the specific namespace.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class ProjectionFactory(
    IReadModelPropertyExpressionResolvers propertyMapperExpressionResolvers,
    IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers,
    IKeyExpressionResolvers keyExpressionResolvers,
    IExpandoObjectConverter expandoObjectConverter,
    IKeyResolvers keyResolvers,
    IStorage storage,
    ILogger<ProjectionFactory> logger) : IProjectionFactory
{
    /// <inheritdoc/>
    public Task<IProjection> Create(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionDefinition definition, ReadModelDefinition readModelDefinition, IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        var eventSequenceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(definition.EventSequenceId);
        return CreateProjectionFrom(
            eventSequenceStorage,
            definition,
            readModelDefinition,
            readModelDefinition.GetSchemaForLatestGeneration(),
            PropertyPath.Root,
            PropertyPath.Root,
            ProjectionPath.GetRootFor(definition.Identifier),
            false,
            eventTypeSchemas);
    }

    static void SetParentOnAllChildProjections(Projection projection, IProjection[] childProjections)
    {
        foreach (var child in childProjections)
        {
            child.SetParent(projection);
        }
    }

    static void SetupRemovedWith(ProjectionDefinition projectionDefinition, PropertyPath childrenAccessorProperty, bool isChild, PropertyPath actualIdentifiedByProperty, Projection projection)
    {
        foreach (var (eventType, _) in projectionDefinition.RemovedWith)
        {
            var observable = projection.Event
                    .WhereEventTypeEquals(eventType);
            if (isChild)
            {
                observable.RemoveChild(
                    childrenAccessorProperty,
                    actualIdentifiedByProperty);
            }
            else
            {
                observable.Remove();
            }
        }
    }

    static void SetupRemovedWithJoin(ProjectionDefinition projectionDefinition, PropertyPath childrenAccessorProperty, PropertyPath actualIdentifiedByProperty, Projection projection)
    {
        foreach (var (eventType, _) in projectionDefinition.RemovedWithJoin)
        {
            projection.Event
                   .WhereEventTypeEquals(eventType)
                   .RemoveChildFromAll(
                       childrenAccessorProperty,
                       actualIdentifiedByProperty);
        }
    }

    static ExpandoObject GetInitialState(IExpandoObjectConverter expandoObjectConverter, ProjectionDefinition projectionDefinition, JsonSchema readModelSchema) =>
        projectionDefinition.InitialModelState.Count == 0 ?
            CreateInitialState(readModelSchema) :
            expandoObjectConverter.ToExpandoObject(projectionDefinition.InitialModelState, readModelSchema);

    static ExpandoObject CreateInitialState(JsonSchema readModelSchema)
    {
        var initialState = new ExpandoObject();
        var stateDict = (IDictionary<string, object?>)initialState;

        foreach (var collection in readModelSchema.Properties.Values.Where(_ => _.IsArray))
        {
            stateDict[collection.Name] = new List<object>();
        }

        return initialState;
    }

    async Task<IProjection> CreateProjectionFrom(
        IEventSequenceStorage eventSequenceStorage,
        ProjectionDefinition projectionDefinition,
        ReadModelDefinition rootReadModel,
        JsonSchema currentReadModelSchema,
        PropertyPath childrenAccessorProperty,
        PropertyPath identifiedByProperty,
        ProjectionPath path,
        bool isChild,
        IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        // Phase 1: Create the projection structure with all parent-child relationships
        var (projection, childProjections, actualIdentifiedByProperty) = await CreateProjectionStructure(
            eventSequenceStorage,
            projectionDefinition.Identifier,
            projectionDefinition,
            rootReadModel,
            currentReadModelSchema,
            childrenAccessorProperty,
            identifiedByProperty,
            path);

        // Phase 2: Resolve events for all projections now that parent relationships are established
        ResolveEventsRecursively(projection, childProjections, projectionDefinition, actualIdentifiedByProperty, isChild);

        // Phase 3: Setup subscriptions for all projections (root and children)
        SetupSubscriptionsRecursively(projection, childProjections, projectionDefinition, childrenAccessorProperty, actualIdentifiedByProperty, currentReadModelSchema, rootReadModel, isChild, eventSequenceStorage, eventTypeSchemas);

        return projection;
    }

    void SetupSubscriptionsRecursively(
        Projection projection,
        IProjection[] childProjections,
        ProjectionDefinition projectionDefinition,
        PropertyPath childrenAccessorProperty,
        PropertyPath actualIdentifiedByProperty,
        JsonSchema currentReadModelSchema,
        ReadModelDefinition rootReadModel,
        bool isChild,
        IEventSequenceStorage eventSequenceStorage,
        IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        // First setup subscriptions for all children (depth-first)
        foreach (var childEntry in projectionDefinition.Children.Zip(childProjections, (kvp, child) => (kvp, child)))
        {
            if (childEntry.child is Projection childProjection)
            {
                var childDefinition = childEntry.kvp.Value;
                var childIdentifiedBy = childDefinition.IdentifiedBy.IsRoot ?
                    childProjection.IdentifiedByProperty :
                    childDefinition.IdentifiedBy;
                var childrenProperty = currentReadModelSchema.Properties[childEntry.kvp.Key.LastSegment.Value]!;
                var childSchema = childrenProperty.Item?.ActualSchema ?? currentReadModelSchema;

                SetupSubscriptionsRecursively(
                    childProjection,
                    childProjection.ChildProjections.ToArray(),
                    childDefinition,
                    childProjection.ChildrenPropertyPath,
                    childIdentifiedBy,
                    childSchema,
                    rootReadModel,
                    true,
                    eventSequenceStorage,
                    eventTypeSchemas);
            }
        }

        // Then setup subscriptions for the current projection
        SetupFromEventPropertyAndJoins(projection, projectionDefinition, childrenAccessorProperty, actualIdentifiedByProperty, currentReadModelSchema, rootReadModel, isChild, eventSequenceStorage, eventTypeSchemas);
    }

    async Task<(Projection Projection, IProjection[] ChildProjections, PropertyPath ActualIdentifiedByProperty)> CreateProjectionStructure(
        IEventSequenceStorage eventSequenceStorage,
        ProjectionId projectionId,
        ProjectionDefinition projectionDefinition,
        ReadModelDefinition rootReadModel,
        JsonSchema currentReadModelSchema,
        PropertyPath childrenAccessorProperty,
        PropertyPath identifiedByProperty,
        ProjectionPath path)
    {
        var schema = rootReadModel.GetSchemaForLatestGeneration();
        var hasIdProperty = schema.HasKeyProperty();
        var actualIdentifiedByProperty = identifiedByProperty.IsRoot && hasIdProperty ? new PropertyPath(schema.GetKeyProperty().Name) : identifiedByProperty;

        // Create child projection structures first (without resolving events)
        var childProjectionTasks = projectionDefinition.Children.Select(async kvp =>
        {
            var childrenProperty = currentReadModelSchema.Properties[kvp.Key.LastSegment.Value]!;
            return await CreateProjectionStructure(
                eventSequenceStorage,
                projectionId,
                kvp.Value,
                rootReadModel,
                childrenProperty.Item?.ActualSchema ?? currentReadModelSchema,
                childrenAccessorProperty.AddArrayIndex(kvp.Key),
                kvp.Value.IdentifiedBy,
                $"{path} -> ChildrenAt({kvp.Key.Path})");
        });

        var childResults = await Task.WhenAll(childProjectionTasks.ToArray());
        var childProjections = childResults.Select(r => r.Projection).Cast<IProjection>().ToArray();
        var initialState = GetInitialState(expandoObjectConverter, projectionDefinition, currentReadModelSchema);

        var projection = new Projection(
            projectionDefinition.EventSequenceId,
            projectionId,
            initialState,
            path,
            childrenAccessorProperty,
            actualIdentifiedByProperty,
            rootReadModel,
            currentReadModelSchema,
            projectionDefinition.IsRewindable,
            projectionDefinition.AutoMap,
            childProjections);

        // Set parent relationships immediately after creation
        // This ensures children have their Parent set before any event resolution
        SetParentOnAllChildProjections(projection, childProjections);

        return (projection, childProjections, actualIdentifiedByProperty);
    }

    void ResolveEventsRecursively(Projection projection, IProjection[] childProjections, ProjectionDefinition projectionDefinition, PropertyPath actualIdentifiedByProperty, bool isChild)
    {
        // First resolve events for all children (depth-first)
        // At this point, all children already have their Parent set
        foreach (var childEntry in projectionDefinition.Children.Zip(childProjections, (kvp, child) => (kvp, child)))
        {
            if (childEntry.child is Projection childProjection)
            {
                var childDefinition = childEntry.kvp.Value;
                var childIdentifiedBy = childDefinition.IdentifiedBy.IsRoot ?
                    childProjection.IdentifiedByProperty :
                    childDefinition.IdentifiedBy;

                ResolveEventsRecursively(
                    childProjection,
                    childProjection.ChildProjections.ToArray(),
                    childDefinition,
                    childIdentifiedBy,
                    true);
            }
        }

        // Then resolve events for the current projection
        ResolveEventsForProjection(projection, childProjections, projectionDefinition, actualIdentifiedByProperty, isChild);
    }

    void SetupFromEventPropertyAndJoins(
        Projection projection,
        ProjectionDefinition projectionDefinition,
        PropertyPath childrenAccessorProperty,
        PropertyPath actualIdentifiedByProperty,
        JsonSchema currentReadModelSchema,
        ReadModelDefinition rootReadModel,
        bool isChild,
        IEventSequenceStorage eventSequenceStorage,
        IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        if (projectionDefinition.FromEventProperty is not null)
        {
            var schemaProperty = rootReadModel.GetSchemaForLatestGeneration().GetSchemaPropertyForPropertyPath(childrenAccessorProperty);
            schemaProperty ??= new JsonSchemaProperty
            {
                Type = projection.TargetReadModelSchema.Type,
                Format = projection.TargetReadModelSchema.Format
            };

            var valueProvider = eventValueProviderExpressionResolvers.Resolve(schemaProperty!, projectionDefinition.FromEventProperty.PropertyExpression);
            projection.Event
                .WhereEventTypeEquals(projectionDefinition.FromEventProperty.Event)
                .AddChildFromEventProperty(childrenAccessorProperty, valueProvider);
        }

        var propertyMappersForEveryEventType = projectionDefinition.FromEvery.Properties.Select(kvp => ResolvePropertyMapper(projection, childrenAccessorProperty + kvp.Key, kvp.Value));
        foreach (var (eventType, fromDefinition) in projectionDefinition.From)
        {
            var fromObservable = SetupFromDefinition(
                projection,
                fromDefinition,
                eventType,
                childrenAccessorProperty,
                actualIdentifiedByProperty,
                propertyMappersForEveryEventType,
                currentReadModelSchema,
                eventTypeSchemas);

            SetupJoinsForFromDefinition(
                fromObservable,
                eventSequenceStorage,
                projectionDefinition,
                childrenAccessorProperty,
                actualIdentifiedByProperty,
                projection,
                currentReadModelSchema,
                fromDefinition,
                isChild);
        }

        SetupRemovedWith(
            projectionDefinition,
            childrenAccessorProperty,
            isChild,
            actualIdentifiedByProperty,
            projection);

        SetupRemovedWithJoin(
            projectionDefinition,
            childrenAccessorProperty,
            actualIdentifiedByProperty,
            projection);

        if (projectionDefinition.FromDerivatives is not null)
        {
            foreach (var fromDerivativesDefinition in projectionDefinition.FromDerivatives)
            {
                foreach (var eventType in fromDerivativesDefinition.EventTypes)
                {
                    var fromObservable = SetupFromDefinition(
                        projection,
                        fromDerivativesDefinition.From,
                        eventType,
                        childrenAccessorProperty,
                        actualIdentifiedByProperty,
                        propertyMappersForEveryEventType,
                        currentReadModelSchema,
                        eventTypeSchemas);

                    SetupJoinsForFromDefinition(
                        fromObservable,
                        eventSequenceStorage,
                        projectionDefinition,
                        childrenAccessorProperty,
                        actualIdentifiedByProperty,
                        projection,
                        currentReadModelSchema,
                        fromDerivativesDefinition.From,
                        isChild);
                }
            }
        }

        foreach (var (eventType, joinDefinition) in projectionDefinition.Join)
        {
            // Auto-expand matching properties if AutoMap is enabled at the projection level
            if (projection.AutoMap == AutoMap.Enabled)
            {
                var eventSchema = eventTypeSchemas.FirstOrDefault(ets => ets.Type == eventType)?.Schema;

                if (eventSchema is not null && currentReadModelSchema is not null)
                {
                    foreach (var eventProperty in eventSchema.Properties.Values)
                    {
                        // Skip properties that are already explicitly mapped
                        if (joinDefinition.Properties.ContainsKey(eventProperty.Name))
                        {
                            continue;
                        }

                        // Look for matching property in read model (case-insensitive)
                        var matchingReadModelProperty = currentReadModelSchema.Properties.Values
                            .FirstOrDefault(rmp => rmp.Name.Equals(eventProperty.Name, StringComparison.OrdinalIgnoreCase));

                        if (matchingReadModelProperty is not null)
                        {
                            // Add automatic mapping from event property to read model property
                            joinDefinition.Properties[matchingReadModelProperty.Name] = eventProperty.Name;
                        }
                    }
                }
            }

            var propertyMappers = joinDefinition.Properties.Select(kvp => ResolvePropertyMapper(projection, childrenAccessorProperty + kvp.Key, kvp.Value)).ToList();
            propertyMappers.AddRange(propertyMappersForEveryEventType);
            var joinObservable = projection.Event
                .WhereEventTypeEquals(eventType)
                .Join(childrenAccessorProperty + joinDefinition.On)
                .Project(
                    childrenAccessorProperty,
                    actualIdentifiedByProperty,
                    propertyMappers);

            if (projectionDefinition.FromEvery.IncludeChildren)
            {
                joinObservable.Project(
                    childrenAccessorProperty,
                    actualIdentifiedByProperty,
                    propertyMappersForEveryEventType);
            }
        }
    }

    IObservable<ProjectionEventContext> SetupFromDefinition(
        Projection projection,
        FromDefinition fromDefinition,
        EventType eventType,
        PropertyPath childrenAccessorProperty,
        PropertyPath actualIdentifiedByProperty,
        IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappersForAllEventTypes,
        JsonSchema currentReadModelSchema,
        IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        // Auto-expand matching properties if AutoMap is enabled at the projection level
        if (projection.AutoMap == AutoMap.Enabled)
        {
            var eventSchema = eventTypeSchemas.FirstOrDefault(ets => ets.Type == eventType)?.Schema;

            if (eventSchema is not null && currentReadModelSchema is not null)
            {
                foreach (var eventProperty in eventSchema.Properties.Values)
                {
                    // Skip properties that are already explicitly mapped
                    if (fromDefinition.Properties.ContainsKey(eventProperty.Name))
                    {
                        continue;
                    }

                    // Look for matching property in read model (case-insensitive)
                    var matchingReadModelProperty = currentReadModelSchema.Properties.Values
                        .FirstOrDefault(rmp => rmp.Name.Equals(eventProperty.Name, StringComparison.OrdinalIgnoreCase));

                    if (matchingReadModelProperty is not null)
                    {
                        // Add automatic mapping from event property to read model property
                        fromDefinition.Properties[matchingReadModelProperty.Name] = eventProperty.Name;
                    }
                }
            }
        }

        var propertyMappers = fromDefinition.Properties.Select(kvp => ResolvePropertyMapper(projection, childrenAccessorProperty + kvp.Key, kvp.Value)).ToList();
        propertyMappers.AddRange(propertyMappersForAllEventTypes);
        return projection.Event
            .WhereEventTypeEquals(eventType)
            .Project(
                childrenAccessorProperty,
                actualIdentifiedByProperty,
                propertyMappers);
    }

    void SetupJoinsForFromDefinition(
        IObservable<ProjectionEventContext> fromObservable,
        IEventSequenceStorage eventSequenceStorage,
        ProjectionDefinition projectionDefinition,
        PropertyPath childrenAccessorProperty,
        PropertyPath actualIdentifiedByProperty,
        Projection projection,
        JsonSchema currentReadModelSchema,
        FromDefinition fromDefinition,
        bool hasParent)
    {
        // Notes: The purpose of this method is to hook up on every From definition that matches the eventType of the Join definition
        // and the join definition matching the property its joining on to then add actions for resolving a join post a projection of
        // the "from".
        var joinExpressions = hasParent
            ? projectionDefinition.Join.Where(join => join.Value.On == actualIdentifiedByProperty).ToArray()
            : projectionDefinition.Join.Where(join => fromDefinition.Properties.Any(from => join.Value.On == from.Key)).ToArray();

        // Include join expressions that join on the id property
        var keyPropertyName = currentReadModelSchema.HasKeyProperty() ? currentReadModelSchema.GetKeyProperty().Name : currentReadModelSchema.GetLikelyKeyPropertyName();
        joinExpressions = [.. joinExpressions, .. projectionDefinition.Join.Where(join => join.Value.On == keyPropertyName)];

        if (joinExpressions.Length == 0)
        {
            return;
        }

        foreach (var (joinEventType, joinDefinition) in joinExpressions)
        {
            var joinPropertyMappers = joinDefinition.Properties.Select(kvp => ResolvePropertyMapper(projection, childrenAccessorProperty + kvp.Key, kvp.Value)).ToArray();
            fromObservable
                .ResolveJoin(eventSequenceStorage, joinEventType, childrenAccessorProperty + joinDefinition.On, logger)
                .Project(
                    childrenAccessorProperty,
                    actualIdentifiedByProperty,
                    joinPropertyMappers);
        }
    }

    PropertyMapper<AppendedEvent, ExpandoObject> ResolvePropertyMapper(Projection projection, PropertyPath propertyPath, string expression)
    {
        var schemaProperty = projection.TargetReadModelSchema.GetSchemaPropertyForPropertyPath(propertyPath);
        if (propertyPath.LastSegment is ThisAccessor)
        {
            schemaProperty = new JsonSchemaProperty
            {
                Type = projection.TargetReadModelSchema.Type,
                Format = projection.TargetReadModelSchema.Format
            };
        }

        return propertyMapperExpressionResolvers.Resolve(propertyPath, schemaProperty!, expression);
    }

    void ResolveEventsForProjection(Projection projection, IProjection[] childProjections, ProjectionDefinition projectionDefinition, PropertyPath actualIdentifiedByProperty, bool hasParent)
    {
        logger.ResolveEventsForProjectionStart(projection.Path, projectionDefinition.From.Count, childProjections.Length);

        // Sets up the key resolver used for root resolution - meaning what identifies the object / document we're working on / projecting to.
        var fromEventTypes = projectionDefinition.From.Select(kvp => GetEventTypeWithKeyResolver(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty, hasParent, kvp.Value.ParentKey)).ToArray();
        var joinEventTypes = projectionDefinition.Join.Select(kvp => GetEventTypeWithKeyResolverForJoin(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty)).ToArray();
        var removedWithEventTypes = projectionDefinition.RemovedWith.Select(kvp => GetEventTypeWithKeyResolver(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty, hasParent, kvp.Value.ParentKey)).ToArray();
        var removedWithJoinEventTypes = projectionDefinition.RemovedWithJoin.Select(kvp => GetEventTypeWithKeyResolverForJoin(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty)).ToArray();

        var operationTypes = fromEventTypes.ToDictionary(_ => _.EventType, _ => ProjectionOperationType.From)
            .Concat(joinEventTypes.ToDictionary(_ => _.EventType, _ => ProjectionOperationType.Join))
            .Concat(removedWithEventTypes.ToDictionary(_ => _.EventType, _ => ProjectionOperationType.Remove))
            .Concat(removedWithJoinEventTypes.ToDictionary(_ => _.EventType, _ => ProjectionOperationType.Join | ProjectionOperationType.Remove))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        List<EventTypeWithKeyResolver> eventsForProjection = [.. fromEventTypes, .. joinEventTypes, .. removedWithEventTypes, .. removedWithJoinEventTypes];
        if (projectionDefinition.FromDerivatives is not null)
        {
            foreach (var fromDerivativeDefinition in projectionDefinition.FromDerivatives)
            {
                eventsForProjection.AddRange(fromDerivativeDefinition.EventTypes.Select(eventType => GetEventTypeWithKeyResolver(projection, eventType, fromDerivativeDefinition.From.Key, actualIdentifiedByProperty, hasParent, fromDerivativeDefinition.From.ParentKey)));
            }
        }

        if (projectionDefinition.FromEventProperty is not null)
        {
            eventsForProjection.Add(new EventTypeWithKeyResolver(projectionDefinition.FromEventProperty.Event, keyResolvers.FromEventSourceId));
        }

        var distinctOwnEventTypes = eventsForProjection.DistinctBy(_ => _.EventType).Select(_ => _.EventType).ToArray();

        foreach (var child in childProjections)
        {
            var childTypes = child.EventTypesWithKeyResolver.Where(_ => !eventsForProjection.Exists(e => e.EventType == _.EventType));
            logger.CollectingEventsFromChild(childTypes.Count(), child.Path);
            eventsForProjection.AddRange(childTypes);
        }

        // TODO: This has an implication in that only one key resolver can exist for each event type, meaning that an event type
        // can only be used once for a projection, including child projections.
        var distinctEventTypes = eventsForProjection.DistinctBy(_ => _.EventType).ToArray();
        logger.ResolveEventsForProjectionComplete(distinctEventTypes.Length, projection.Path);
        projection.SetEventTypesWithKeyResolvers(
            distinctEventTypes,
            distinctOwnEventTypes,
            operationTypes);
    }

    EventTypeWithKeyResolver GetEventTypeWithKeyResolverForJoin(IProjection projection, EventType eventType, PropertyExpression key, PropertyPath actualIdentifiedByProperty)
    {
        var keyResolver = GetKeyResolverFor(projection, key, actualIdentifiedByProperty);
        keyResolver = keyResolvers.ForJoin(projection, keyResolver, actualIdentifiedByProperty);
        return new EventTypeWithKeyResolver(eventType, keyResolver);
    }

    EventTypeWithKeyResolver GetEventTypeWithKeyResolver(Projection projection, EventType eventType, PropertyExpression key, PropertyPath actualIdentifiedByProperty, bool hasParent, PropertyExpression? parentKey)
    {
        logger.GetEventTypeWithKeyResolverStart(
            eventType.Id.ToString(),
            hasParent,
            parentKey?.Value,
            projection.HasParent,
            projection.HasParent ? projection.Parent!.IdentifiedByProperty.Path : "N/A");

        var keyResolver = GetKeyResolverFor(projection, key, actualIdentifiedByProperty);
        if (!hasParent)
        {
            return new EventTypeWithKeyResolver(eventType, keyResolver);
        }

        // Default to EventSourceId for parent key when not explicitly specified
        // This supports the parent-keyed pattern where the EventSourceId represents the parent's identifier
        var effectiveParentKey = parentKey;
        if (effectiveParentKey is null || string.IsNullOrEmpty(effectiveParentKey.Value))
        {
            effectiveParentKey = new PropertyExpression(WellKnownExpressions.EventSourceId);
            logger.GetEventTypeWithKeyResolverInferredParentKey(effectiveParentKey.Value, true);
        }

        var parentKeyResolver = GetParentKeyResolverFor(projection, effectiveParentKey, actualIdentifiedByProperty);
        keyResolver = keyResolvers.FromParentHierarchy(projection, keyResolver, parentKeyResolver, actualIdentifiedByProperty);

        return new EventTypeWithKeyResolver(eventType, keyResolver);
    }

    KeyResolver GetKeyResolverFor(IProjection projection, PropertyExpression? key, PropertyPath actualIdentifiedByProperty)
    {
        if (key is not null && key.Value.Length != 0 && keyExpressionResolvers.CanResolve(key))
        {
            return keyExpressionResolvers.Resolve(projection, key, actualIdentifiedByProperty);
        }

        return keyResolvers.FromEventSourceId;
    }

    KeyResolver GetParentKeyResolverFor(IProjection projection, PropertyExpression? key, PropertyPath actualIdentifiedByProperty)
    {
        logger.GetParentKeyResolverFor(key?.Value, false);

        if (key is not null && key.Value.Length != 0 && keyExpressionResolvers.CanResolve(key))
        {
            return keyExpressionResolvers.Resolve(projection, key, actualIdentifiedByProperty);
        }

        return keyResolvers.FromEventSourceId;
    }
}
