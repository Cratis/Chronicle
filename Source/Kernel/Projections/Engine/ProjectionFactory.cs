// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections.Definitions;
using Cratis.Events.Projections.Expressions;
using Cratis.Properties;
using NJsonSchema;

namespace Cratis.Events.Projections
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
        public IProjection CreateFrom(ProjectionDefinition definition) =>
            CreateProjectionFrom(
                definition.Name,
                definition,
                PropertyPath.Root,
                PropertyPath.Root,
                $"Root({definition.Identifier})",
                new Dictionary<PropertyPath, ChildrenDefinition>(),
                _ => { });

        IProjection CreateProjectionFrom(
            ProjectionName name,
            ProjectionDefinition projectionDefinition,
            PropertyPath childrenAccessorProperty,
            PropertyPath identifiedByProperty,
            ProjectionPath path,
            IDictionary<PropertyPath, ChildrenDefinition> childrenDefinitions,
            Action<IEnumerable<EventTypeWithKeyResolver>> addChildEventTypes)
        {
            var eventsForProjection = projectionDefinition.From.Select(kvp => new EventTypeWithKeyResolver(
                kvp.Key,
                string.IsNullOrEmpty(kvp.Value.ParentKey) ? EventValueProviders.FromEventSourceId : EventValueProviders.FromEventContent(kvp.Value.ParentKey!))).ToList();

            var childProjections = projectionDefinition.Children.Select(kvp => CreateProjectionFrom(
                    name,
                    kvp.Value,
                    kvp.Key,
                    kvp.Value.IdentifiedBy,
                    $"{path} -> ChildrenAt({kvp.Key.Path})",
                    projectionDefinition.Children,
                    _ =>
                    {
                        eventsForProjection.AddRange(_);
                        addChildEventTypes(_);
                    })).ToArray();

            var task = JsonSchema.FromJsonAsync(projectionDefinition.Model.Schema);
            task.Wait();

            var model = new Model(projectionDefinition.Model.Name, task.Result);
            addChildEventTypes(eventsForProjection);

            var projection = new Projection(projectionDefinition.Identifier, name, path, model, eventsForProjection, childProjections);

            foreach (var (childrenProperty, childrenDefinition) in childrenDefinitions)
            {
                foreach (var (eventType, fromDefinition) in childrenDefinition.From)
                {
                    var propertyMappers = fromDefinition.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(kvp.Key, kvp.Value));
                    var keyResolver = string.IsNullOrEmpty(fromDefinition.ParentKey) ? EventValueProviders.FromEventContent(childrenDefinition.IdentifiedBy) : EventValueProviders.FromEventSourceId;
                    projection.Event.From(eventType).Child(childrenProperty, childrenDefinition.IdentifiedBy, keyResolver, propertyMappers);
                }
            }

            foreach (var (eventType, fromDefinition) in projectionDefinition.From)
            {
                var propertyMappers = fromDefinition.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(kvp.Key, kvp.Value));
                projection.Event.From(eventType).Project(childrenAccessorProperty, identifiedByProperty, EventValueProviders.FromEventSourceId, propertyMappers);
            }

            return projection;
        }
    }
}
