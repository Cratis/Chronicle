// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Expressions;
using Aksio.Cratis.Events.Projections.Expressions.Keys;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Json;
using Aksio.Cratis.Properties;
using NJsonSchema;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionFactory"/>.
/// </summary>
public class ProjectionFactory : IProjectionFactory
{
    readonly IModelPropertyExpressionResolvers _propertyMapperExpressionResolvers;
    readonly IKeyExpressionResolvers _keyExpressionResolvers;
    readonly IEventSequenceStorageProvider _eventProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionFactory"/> class.
    /// </summary>
    /// <param name="propertyMapperExpressionResolvers"><see cref="IModelPropertyExpressionResolvers"/> for resolving expressions for properties.</param>
    /// <param name="keyExpressionResolvers"><see cref="IKeyExpressionResolvers"/> for resolving keys.</param>
    /// <param name="eventProvider"><see cref="IEventSequenceStorageProvider"/> for providing events from the event store.</param>
    public ProjectionFactory(
        IModelPropertyExpressionResolvers propertyMapperExpressionResolvers,
        IKeyExpressionResolvers keyExpressionResolvers,
        IEventSequenceStorageProvider eventProvider)
    {
        _propertyMapperExpressionResolvers = propertyMapperExpressionResolvers;
        _keyExpressionResolvers = keyExpressionResolvers;
        _eventProvider = eventProvider;
    }

    /// <inheritdoc/>
    public Task<IProjection> CreateFrom(ProjectionDefinition definition) =>
        CreateProjectionFrom(
            definition.Name,
            definition,
            PropertyPath.Root,
            PropertyPath.Root,
            ProjectionPath.GetRootFor(definition.Identifier),
            false);

    async Task<IProjection> CreateProjectionFrom(
        ProjectionName name,
        ProjectionDefinition projectionDefinition,
        PropertyPath childrenAccessorProperty,
        PropertyPath identifiedByProperty,
        ProjectionPath path,
        bool hasParent)
    {
        var actualIdentifiedByProperty = identifiedByProperty.IsRoot ? new PropertyPath("_id") : identifiedByProperty;

        var childProjectionTasks = projectionDefinition.Children.Select(async kvp => await CreateProjectionFrom(
                name,
                kvp.Value,
                childrenAccessorProperty.AddArrayIndex(kvp.Key),
                kvp.Value.IdentifiedBy,
                $"{path} -> ChildrenAt({kvp.Key.Path})",
                true));

        var childProjections = await Task.WhenAll(childProjectionTasks.ToArray());
        var modelSchema = await JsonSchema.FromJsonAsync(projectionDefinition.Model.Schema);
        var model = new Model(projectionDefinition.Model.Name, modelSchema);

        var projection = new Projection(
            projectionDefinition.Identifier,
            projectionDefinition.InitialModelState?.AsExpandoObject() ?? new ExpandoObject(),
            name,
            path,
            childrenAccessorProperty,
            model,
            projectionDefinition.IsRewindable,
            childProjections);

        SetParentOnAllChildProjections(projection, childProjections);
        ResolveEventsForProjection(projection, childProjections, projectionDefinition, actualIdentifiedByProperty, hasParent);

        var propertyMappersForAllEventTypes = projectionDefinition.All.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(childrenAccessorProperty + kvp.Key, kvp.Value));
        foreach (var (eventType, fromDefinition) in projectionDefinition.From)
        {
            var joinExpressions = projectionDefinition.Join.Where(join => fromDefinition.Properties.Any(from => join.Value.On == from.Key));
            var propertyMappers = fromDefinition.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(childrenAccessorProperty + kvp.Key, kvp.Value)).ToList();
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
                    var joinPropertyMappers = joinDefinition.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(childrenAccessorProperty + kvp.Key, kvp.Value)).ToArray();
                    projected = projected
                        .ResolveJoin(_eventProvider, joinEventType, joinDefinition.On)
                        .Project(
                            childrenAccessorProperty,
                            actualIdentifiedByProperty,
                            joinPropertyMappers);
                }
            }
        }

        foreach (var (eventType, joinDefinition) in projectionDefinition.Join)
        {
            var propertyMappers = joinDefinition.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(childrenAccessorProperty + kvp.Key, kvp.Value)).ToList();
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

    void ResolveEventsForProjection(IProjection projection, IProjection[] childProjections, ProjectionDefinition projectionDefinition, PropertyPath actualIdentifiedByProperty, bool hasParent)
    {
        // Sets up the key resolver used for root resolution - meaning what identifies the object / document we're working on / projecting to.
        var eventsForProjection = projectionDefinition.From.Select(kvp => GetEventTypeWithKeyResolverFor(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty, hasParent, kvp.Value.ParentKey)).ToList();
        eventsForProjection.AddRange(projectionDefinition.Join.Select(kvp => GetEventTypeWithKeyResolverFor(projection, kvp.Key, kvp.Value.Key, actualIdentifiedByProperty)));

        if (projectionDefinition.RemovedWith != default)
        {
            eventsForProjection.Add(new EventTypeWithKeyResolver(projectionDefinition.RemovedWith.Event, KeyResolvers.FromEventSourceId));
            projection.Event.RemovedWith(projectionDefinition.RemovedWith.Event);
        }

        foreach (var child in childProjections)
        {
            eventsForProjection.AddRange(child.EventTypesWithKeyResolver);
        }
        projection.SetEventTypesWithKeyResolvers(eventsForProjection.DistinctBy(_ => _.EventType).ToArray());
    }

    void SetParentOnAllChildProjections(Projection projection, IProjection[] childProjections)
    {
        foreach (var child in childProjections)
        {
            child.SetParent(projection);
        }
    }

    EventTypeWithKeyResolver GetEventTypeWithKeyResolverFor(IProjection projection, EventType eventType, string key, PropertyPath actualIdentifiedByProperty, bool hasParent = false, string? parentKey = null)
    {
        var keyResolver = GetKeyResolverFor(projection, key, actualIdentifiedByProperty);
        if (hasParent)
        {
            var parentKeyResolver = GetKeyResolverFor(projection, parentKey, actualIdentifiedByProperty);
            keyResolver = KeyResolvers.FromParentHierarchy(projection, keyResolver, parentKeyResolver, actualIdentifiedByProperty);
        }

        return new EventTypeWithKeyResolver(eventType, keyResolver);
    }

    KeyResolver GetKeyResolverFor(IProjection projection, string? key, PropertyPath actualIdentifiedByProperty)
    {
        if (!string.IsNullOrEmpty(key) && _keyExpressionResolvers.CanResolve(key))
        {
            return _keyExpressionResolvers.Resolve(projection, key, actualIdentifiedByProperty);
        }

        return KeyResolvers.FromEventSourceId;
    }
}
