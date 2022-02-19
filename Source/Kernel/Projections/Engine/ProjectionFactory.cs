// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Expressions;
using Aksio.Cratis.Properties;
using NJsonSchema;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionFactory"/>.
    /// </summary>
    public class ProjectionFactory : IProjectionFactory
    {
        readonly IPropertyMapperExpressionResolvers _propertyMapperExpressionResolvers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionFactory"/> class.
        /// </summary>
        /// <param name="propertyMapperExpressionResolvers"><see cref="IPropertyMapperExpressionResolvers"/> for resolving expressions for properties.</param>
        public ProjectionFactory(IPropertyMapperExpressionResolvers propertyMapperExpressionResolvers)
        {
            _propertyMapperExpressionResolvers = propertyMapperExpressionResolvers;
        }

        /// <inheritdoc/>
        public Task<IProjection> CreateFrom(ProjectionDefinition definition) =>
            CreateProjectionFrom(
                definition.Name,
                definition,
                PropertyPath.Root,
                PropertyPath.Root,
                ProjectionPath.GetRootFor(definition.Identifier));

        async Task<IProjection> CreateProjectionFrom(
            ProjectionName name,
            ProjectionDefinition projectionDefinition,
            PropertyPath childrenAccessorProperty,
            PropertyPath identifiedByProperty,
            ProjectionPath path)
        {
            var actualIdentifiedByProperty = identifiedByProperty.IsRoot ? new PropertyPath("_id") : identifiedByProperty;

            var childProjectionTasks = projectionDefinition.Children.Select(async kvp => await CreateProjectionFrom(
                    name,
                    kvp.Value,
                    childrenAccessorProperty.AddArrayIndex(kvp.Key),
                    kvp.Value.IdentifiedBy,
                    $"{path} -> ChildrenAt({kvp.Key.Path})"));

            var childProjections = await Task.WhenAll(childProjectionTasks.ToArray());

            var modelSchema = await JsonSchema.FromJsonAsync(projectionDefinition.Model.Schema);
            var model = new Model(projectionDefinition.Model.Name, modelSchema);

            var projection = new Projection(
                projectionDefinition.Identifier,
                name,
                path,
                childrenAccessorProperty,
                model,
                projectionDefinition.IsPassive,
                projectionDefinition.IsRewindable,
                childProjections);

            // Sets up the key resolver used for root resolution - meaning what identifies the object / document we're working on / projecting to.
            var eventsForProjection = projectionDefinition.From.Select(kvp => GetEventTypeWithKeyResolverFor(projection, kvp.Key, kvp.Value, actualIdentifiedByProperty)).ToList();

            if (projectionDefinition.RemovedWith != default)
            {
                eventsForProjection.Add(new EventTypeWithKeyResolver(projectionDefinition.RemovedWith.Event, KeyResolvers.FromEventSourceId()));
                projection.Event.RemovedWith(projectionDefinition.RemovedWith.Event);
            }

            foreach (var child in childProjections)
            {
                child.SetParent(projection);
                eventsForProjection.AddRange(child.EventTypesWithKeyResolver);
            }
            projection.SetEventTypesWithKeyResolvers(eventsForProjection.DistinctBy(_ => _.EventType));

            foreach (var (eventType, fromDefinition) in projectionDefinition.From)
            {
                var propertyMappers = fromDefinition.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(childrenAccessorProperty + kvp.Key, kvp.Value)).ToArray();
                projection.Event
                    .From(eventType)
                    .Project(
                        childrenAccessorProperty,
                        actualIdentifiedByProperty,
                        propertyMappers);
            }

            return projection;
        }

        EventTypeWithKeyResolver GetEventTypeWithKeyResolverFor(IProjection projection, EventType eventType, FromDefinition from, PropertyPath actualIdentifiedByProperty)
        {
            KeyResolver keyResolver;
            if (!string.IsNullOrEmpty(from.ParentKey))
            {
                keyResolver = KeyResolvers.FromParentHierarchy(projection, from.ParentKey!, actualIdentifiedByProperty);
            }
            else if (!string.IsNullOrEmpty(from.Key))
            {
                keyResolver = KeyResolvers.FromEventContent(projection, from.Key, actualIdentifiedByProperty);
            }
            else
            {
                keyResolver = KeyResolvers.FromEventSourceId();
            }

            return new EventTypeWithKeyResolver(eventType, keyResolver);
        }
    }
}
