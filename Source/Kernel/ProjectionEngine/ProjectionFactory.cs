// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProjectionEngine.Expressions;
using Cratis.Chronicle.ProjectionEngine.Expressions.EventValues;
using Cratis.Chronicle.ProjectionEngine.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using NJsonSchema;

namespace Cratis.Chronicle.ProjectionEngine;

/// <summary>
/// Represents an implementation of <see cref="IProjectionFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionFactory"/> class.
/// </remarks>
/// <param name="propertyMapperExpressionResolvers"><see cref="IModelPropertyExpressionResolvers"/> for resolving expressions for properties.</param>
/// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving expressions for accessing values on events.</param>
/// <param name="keyExpressionResolvers"><see cref="IKeyExpressionResolvers"/> for resolving keys.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting to and from expando objects.</param>
/// <param name="keyResolvers"><see cref="IKeyResolvers"/> for resolving <see cref="Key"/>.</param>
/// <param name="storage"><see cref="IEventStoreNamespaceStorage"/> for accessing underlying storage for the specific namespace.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class ProjectionFactory(
    IModelPropertyExpressionResolvers propertyMapperExpressionResolvers,
    IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers,
    IKeyExpressionResolvers keyExpressionResolvers,
    IExpandoObjectConverter expandoObjectConverter,
    IKeyResolvers keyResolvers,
    IStorage storage,
    ILogger<ProjectionFactory> logger) : IProjectionFactory
{
    /// <inheritdoc/>
    public Task<IProjection> Create(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionDefinition definition)
    {
        var eventSequenceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(definition.EventSequenceId);
        return CreateProjectionFrom(
            eventSequenceStorage,
            definition,
            PropertyPath.Root,
            PropertyPath.Root,
            ProjectionPath.GetRootFor(definition.Identifier),
            false);
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

    static ExpandoObject GetInitialState(IExpandoObjectConverter expandoObjectConverter, ProjectionDefinition projectionDefinition, JsonSchema modelSchema, Model model) =>
        projectionDefinition.InitialModelState.Count == 0 ?
            CreateInitialState(model) :
            expandoObjectConverter.ToExpandoObject(projectionDefinition.InitialModelState, modelSchema);

    static ExpandoObject CreateInitialState(Model model)
    {
        // If there is no initial state, we create one with empty collections for all arrays.
        // This is to ensure that we can add to them without having to check for null.
        // And that any sinks don't fail when trying to access them.
        var initialState = new ExpandoObject();
        foreach (var collection in model.Schema.GetFlattenedProperties().Where(_ => _.IsArray))
        {
            ((IDictionary<string, object?>)initialState)[collection.Name] = new List<object>();
        }

        return initialState;
    }

    async Task<IProjection> CreateProjectionFrom(
        IEventSequenceStorage eventSequenceStorage,
        ProjectionDefinition projectionDefinition,
        PropertyPath childrenAccessorProperty,
        PropertyPath identifiedByProperty,
        ProjectionPath path,
        bool isChild)
    {
        var modelSchema = await JsonSchema.FromJsonAsync(projectionDefinition.Model.Schema);
        var model = new Model(projectionDefinition.Model.Name, modelSchema);
        var hasIdProperty = modelSchema.GetFlattenedProperties().Any(_ => _.Name == "id");
        var actualIdentifiedByProperty = identifiedByProperty.IsRoot && hasIdProperty ? new PropertyPath("id") : identifiedByProperty;

        var childProjectionTasks = projectionDefinition.Children.Select(async kvp => await CreateProjectionFrom(
                eventSequenceStorage,
                kvp.Value,
                childrenAccessorProperty.AddArrayIndex(kvp.Key),
                kvp.Value.IdentifiedBy,
                $"{path} -> ChildrenAt({kvp.Key.Path})",
                true));

        var childProjections = await Task.WhenAll(childProjectionTasks.ToArray());
        var initialState = GetInitialState(expandoObjectConverter, projectionDefinition, modelSchema, model);

        var projection = new Projection(
            projectionDefinition.EventSequenceId,
            projectionDefinition.Identifier,
            projectionDefinition.Sink,
            initialState,
            path,
            childrenAccessorProperty,
            model,
            projectionDefinition.IsRewindable,
            childProjections);

        SetParentOnAllChildProjections(projection, childProjections);
        ResolveEventsForProjection(projection, childProjections, projectionDefinition, actualIdentifiedByProperty, isChild);

        if (projectionDefinition.FromEventProperty is not null)
        {
            var schemaProperty = model.Schema.GetSchemaPropertyForPropertyPath(childrenAccessorProperty);
            schemaProperty ??= new JsonSchemaProperty
            {
                Type = projection.Model.Schema.Type,
                Format = projection.Model.Schema.Format
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
                propertyMappersForEveryEventType);

            SetupJoinsForFromDefinition(
                fromObservable,
                eventSequenceStorage,
                projectionDefinition,
                childrenAccessorProperty,
                actualIdentifiedByProperty,
                projection,
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
                        propertyMappersForEveryEventType);

                    SetupJoinsForFromDefinition(
                        fromObservable,
                        eventSequenceStorage,
                        projectionDefinition,
                        childrenAccessorProperty,
                        actualIdentifiedByProperty,
                        projection,
                        fromDerivativesDefinition.From,
                        isChild);
                }
            }
        }

        foreach (var (eventType, joinDefinition) in projectionDefinition.Join)
        {
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

        return projection;
    }

    IObservable<ProjectionEventContext> SetupFromDefinition(
        Projection projection,
        FromDefinition fromDefinition,
        EventType eventType,
        PropertyPath childrenAccessorProperty,
        PropertyPath actualIdentifiedByProperty,
        IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappersForAllEventTypes)
    {
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
        joinExpressions = [.. joinExpressions, .. projectionDefinition.Join.Where(join => join.Value.On == "id")];

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
                    joinPropertyMappers)
                .Optimize();
        }
    }

    PropertyMapper<AppendedEvent, ExpandoObject> ResolvePropertyMapper(Projection projection, PropertyPath propertyPath, string expression)
    {
        var schemaProperty = projection.Model.Schema.GetSchemaPropertyForPropertyPath(propertyPath);
        if (propertyPath.LastSegment is ThisAccessor)
        {
            schemaProperty = new JsonSchemaProperty
            {
                Type = projection.Model.Schema.Type,
                Format = projection.Model.Schema.Format
            };
        }

        return propertyMapperExpressionResolvers.Resolve(propertyPath, schemaProperty!, expression);
    }

    void ResolveEventsForProjection(Projection projection, IProjection[] childProjections, ProjectionDefinition projectionDefinition, PropertyPath actualIdentifiedByProperty, bool hasParent)
    {
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
            eventsForProjection.AddRange(childTypes);
        }

        // TODO: This has an implication in that only one key resolver can exist for each event type, meaning that an event type
        // can only be used once for a projection, including child projections.
        var distinctEventTypes = eventsForProjection.DistinctBy(_ => _.EventType).ToArray();
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

    EventTypeWithKeyResolver GetEventTypeWithKeyResolver(IProjection projection, EventType eventType, PropertyExpression key, PropertyPath actualIdentifiedByProperty, bool hasParent, PropertyExpression? parentKey)
    {
        var keyResolver = GetKeyResolverFor(projection, key, actualIdentifiedByProperty);
        if (!hasParent)
        {
            return new EventTypeWithKeyResolver(eventType, keyResolver);
        }
        var parentKeyResolver = GetKeyResolverFor(projection, parentKey, actualIdentifiedByProperty);
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
}
