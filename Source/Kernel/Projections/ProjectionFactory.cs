// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Models;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections.Expressions;
using Cratis.Chronicle.Projections.Expressions.EventValues;
using Cratis.Chronicle.Projections.Expressions.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.DependencyInjection;
using NJsonSchema;

namespace Cratis.Chronicle.Projections;

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
/// <param name="storage"><see cref="IEventStoreNamespaceStorage"/> for accessing underlying storage for the specific namespace.</param>
[Singleton]
public class ProjectionFactory(
    IModelPropertyExpressionResolvers propertyMapperExpressionResolvers,
    IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers,
    IKeyExpressionResolvers keyExpressionResolvers,
    IExpandoObjectConverter expandoObjectConverter,
    IStorage storage) : IProjectionFactory
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

    async Task<IProjection> CreateProjectionFrom(
        IEventSequenceStorage eventSequenceStorage,
        ProjectionDefinition projectionDefinition,
        PropertyPath childrenAccessorProperty,
        PropertyPath identifiedByProperty,
        ProjectionPath path,
        bool hasParent)
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

        ExpandoObject initialState;
        if (projectionDefinition.InitialModelState is not null)
        {
            initialState = expandoObjectConverter.ToExpandoObject(projectionDefinition.InitialModelState, modelSchema);
        }
        else
        {
            initialState = new ExpandoObject();
        }

        var projection = new Projection(
            projectionDefinition.Identifier,
            initialState,
            path,
            childrenAccessorProperty,
            model,
            projectionDefinition.IsRewindable,
            childProjections);

        SetParentOnAllChildProjections(projection, childProjections);
        ResolveEventsForProjection(projection, childProjections, projectionDefinition, actualIdentifiedByProperty, hasParent);

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

        var propertyMappersForAllEventTypes = projectionDefinition.All.Properties.Select(kvp => ResolvePropertyMapper(projection, childrenAccessorProperty + kvp.Key, kvp.Value));
        foreach (var (eventType, fromDefinition) in projectionDefinition.From)
        {
            SetupFromSubscription(eventSequenceStorage, projectionDefinition, childrenAccessorProperty, actualIdentifiedByProperty, projection, propertyMappersForAllEventTypes, eventType, fromDefinition);
        }

        if (projectionDefinition.FromAny is not null)
        {
            foreach (var fromAnyDefinition in projectionDefinition.FromAny)
            {
                foreach (var eventType in fromAnyDefinition.EventTypes)
                {
                    SetupFromSubscription(eventSequenceStorage, projectionDefinition, childrenAccessorProperty, actualIdentifiedByProperty, projection, propertyMappersForAllEventTypes, eventType, fromAnyDefinition.From);
                }
            }
        }

        foreach (var (eventType, joinDefinition) in projectionDefinition.Join)
        {
            var propertyMappers = joinDefinition.Properties.Select(kvp => ResolvePropertyMapper(projection, childrenAccessorProperty + kvp.Key, kvp.Value)).ToList();
            propertyMappers.AddRange(propertyMappersForAllEventTypes);
            projection.Event
                .WhereEventTypeEquals(eventType)
                .Join(joinDefinition.On)
                .Project(
                    childrenAccessorProperty,
                    actualIdentifiedByProperty,
                    propertyMappers);
        }

        if (projectionDefinition.All.IncludeChildren)
        {
            var childEventTypes = projection
                .EventTypes
                .Where(_ => !projectionDefinition.From.Any(kvp => kvp.Key == _) && !projectionDefinition.Join.Any(kvp => kvp.Key == _));

            foreach (var eventType in childEventTypes)
            {
                projection.Event
                    .WhereEventTypeEquals(eventType)
                    .Project(
                        childrenAccessorProperty,
                        actualIdentifiedByProperty,
                        propertyMappersForAllEventTypes);
            }
        }

        return projection;
    }

    void SetupFromSubscription(
        IEventSequenceStorage eventSequenceStorage,
        ProjectionDefinition projectionDefinition,
        PropertyPath childrenAccessorProperty,
        PropertyPath actualIdentifiedByProperty,
        Projection projection,
        IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappersForAllEventTypes,
        EventType eventType,
        FromDefinition fromDefinition)
    {
        var joinExpressions = projectionDefinition.Join.Where(join => fromDefinition.Properties.Any(from => join.Value.On == from.Key));
        var propertyMappers = fromDefinition.Properties.Select(kvp => ResolvePropertyMapper(projection, childrenAccessorProperty + kvp.Key, kvp.Value)).ToList();
        propertyMappers.AddRange(propertyMappersForAllEventTypes);
        var projected = projection.Event
            .WhereEventTypeEquals(eventType)
            .Project(
                childrenAccessorProperty,
                actualIdentifiedByProperty,
                propertyMappers);

        if (joinExpressions.Any())
        {
            foreach (var (joinEventType, joinDefinition) in joinExpressions)
            {
                var joinPropertyMappers = joinDefinition.Properties.Select(kvp => ResolvePropertyMapper(projection, childrenAccessorProperty + kvp.Key, kvp.Value)).ToArray();
                projected = projected
                    .ResolveJoin(eventSequenceStorage, joinEventType, joinDefinition.On)
                    .Project(
                        childrenAccessorProperty,
                        actualIdentifiedByProperty,
                        joinPropertyMappers);
            }
        }
    }

    PropertyMapper<AppendedEvent, ExpandoObject> ResolvePropertyMapper(IProjection projection, PropertyPath propertyPath, string expression)
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

    void ResolveEventsForProjection(IProjection projection, IProjection[] childProjections, ProjectionDefinition projectionDefinition, PropertyPath actualIdentifiedByProperty, bool hasParent)
    {
        // Sets up the key resolver used for root resolution - meaning what identifies the object / document we're working on / projecting to.
        var eventsForProjection = projectionDefinition.From.Select(kvp => GetEventTypeWithKeyResolverFor(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty, hasParent, kvp.Value.ParentKey)).ToList();
        eventsForProjection.AddRange(projectionDefinition.Join.Select(kvp => GetEventTypeWithKeyResolverFor(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty)));

        if (projectionDefinition.FromAny is not null)
        {
            foreach (var fromAnyDefinition in projectionDefinition.FromAny)
            {
                eventsForProjection.AddRange(fromAnyDefinition.EventTypes.Select(eventType => GetEventTypeWithKeyResolverFor(projection, eventType, fromAnyDefinition.From.Key, actualIdentifiedByProperty, hasParent, fromAnyDefinition.From.ParentKey)));
            }
        }

        if (projectionDefinition.FromEventProperty is not null)
        {
            eventsForProjection.Add(new EventTypeWithKeyResolver(projectionDefinition.FromEventProperty.Event, KeyResolvers.FromEventSourceId));
        }

        if (projectionDefinition.RemovedWith is not null)
        {
            eventsForProjection.Add(new EventTypeWithKeyResolver(projectionDefinition.RemovedWith.Event, KeyResolvers.FromEventSourceId));
            projection.Event.RemovedWith(projectionDefinition.RemovedWith.Event);
        }
        var distinctOwnEventTypes = eventsForProjection.DistinctBy(_ => _.EventType).Select(_ => _.EventType).ToArray();

        foreach (var child in childProjections)
        {
            var childTypes = child.EventTypesWithKeyResolver.Where(_ => !eventsForProjection.Exists(e => e.EventType == _.EventType));
            eventsForProjection.AddRange(childTypes);
        }
        var distinctEventTypes = eventsForProjection.DistinctBy(_ => _.EventType).ToArray();
        projection.SetEventTypesWithKeyResolvers(distinctEventTypes, distinctOwnEventTypes);
    }

    void SetParentOnAllChildProjections(Projection projection, IProjection[] childProjections)
    {
        foreach (var child in childProjections)
        {
            child.SetParent(projection);
        }
    }

    EventTypeWithKeyResolver GetEventTypeWithKeyResolverFor(IProjection projection, EventType eventType, PropertyExpression key, PropertyPath actualIdentifiedByProperty, bool hasParent = false, PropertyExpression? parentKey = null)
    {
        var keyResolver = GetKeyResolverFor(projection, key, actualIdentifiedByProperty);
        if (hasParent)
        {
            var parentKeyResolver = GetKeyResolverFor(projection, parentKey, actualIdentifiedByProperty);
            keyResolver = KeyResolvers.FromParentHierarchy(projection, keyResolver, parentKeyResolver, actualIdentifiedByProperty);
        }

        return new EventTypeWithKeyResolver(eventType, keyResolver);
    }

    KeyResolver GetKeyResolverFor(IProjection projection, PropertyExpression? key, PropertyPath actualIdentifiedByProperty)
    {
        if (key is not null && key.Value.Length != 0 && keyExpressionResolvers.CanResolve(key))
        {
            return keyExpressionResolvers.Resolve(projection, key, actualIdentifiedByProperty);
        }

        return KeyResolvers.FromEventSourceId;
    }
}
