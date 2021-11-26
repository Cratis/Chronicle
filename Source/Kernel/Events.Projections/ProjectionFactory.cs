// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections.Definitions;
using Cratis.Events.Projections.Expressions;
using Cratis.Properties;
using Newtonsoft.Json.Schema;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="IProjectionFactory"/>.
    /// </summary>
    public class ProjectionFactory : IProjectionFactory
    {
        readonly IPropertyMapperExpressionResolvers _propertyMapperExpressionResolvers;

        public ProjectionFactory(IPropertyMapperExpressionResolvers propertyMapperExpressionResolvers)
        {
            _propertyMapperExpressionResolvers = propertyMapperExpressionResolvers;
        }

        /// <inheritdoc/>
        public IProjection CreateFrom(ProjectionDefinition definition) =>
            CreateProjectionFrom(
                definition,
                PropertyPath.Root,
                PropertyPath.Root,
                $"Root({definition.Identifier})",
                new Dictionary<PropertyPath, ChildrenDefinition>(),
                _ => { });

        IProjection CreateProjectionFrom(
            ProjectionDefinition projectionDefinition,
            PropertyPath childrenAccessorProperty,
            PropertyPath identifiedByProperty,
            ProjectionPath path,
            IDictionary<PropertyPath, ChildrenDefinition> childrenDefinitions,
            Action<IEnumerable<EventTypeWithKeyResolver>> addChildEventTypes)
        {
            var eventsForProjection = projectionDefinition.From.Select(kvp => new EventTypeWithKeyResolver(kvp.Key, string.IsNullOrEmpty(kvp.Value.ParentKey) ?
                    EventValueProviders.FromEventSourceId :
                    EventValueProviders.FromEventContent(kvp.Value.ParentKey!))).ToList();

            var childProjections = projectionDefinition.Children.Select(kvp => CreateProjectionFrom(
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

            var model = new Model(projectionDefinition.Model.Name, JSchema.Parse(projectionDefinition.Model.Schema));
            addChildEventTypes(eventsForProjection);

            var projection = new Projection(projectionDefinition.Identifier, projectionDefinition.Name, path, model, eventsForProjection, childProjections);

            foreach (var (childrenProperty, childrenDefinition) in childrenDefinitions)
            {
                foreach (var (eventType, fromDefinition) in childrenDefinition.From)
                {
                    var propertyMappers = fromDefinition.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(kvp.Key, kvp.Value));
                    projection.Event.From(eventType).Child(childrenProperty, childrenDefinition.IdentifiedBy, EventValueProviders.FromEventSourceId, propertyMappers);
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
