// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Concepts;
using Cratis.Events.Projections.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents a parser for JSON definition of a <see cref="IProjection"/>.
    /// </summary>
    public class JsonProjectionParser
    {
        readonly IPropertyMapperExpressionResolvers _propertyMapperExpressionResolvers;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonProjectionParser"/>.
        /// </summary>
        /// <param name="propertyMapperExpressionResolvers"><see cref="IPropertyMapperExpressionResolvers"/> for resolving event value expressions.</param>
        public JsonProjectionParser(IPropertyMapperExpressionResolvers propertyMapperExpressionResolvers)
        {
            _propertyMapperExpressionResolvers = propertyMapperExpressionResolvers;
        }

        /// <summary>
        /// Parse a JSON string definition and produce a <see cref="IProjection"/>.
        /// </summary>
        /// <param name="json">JSON to parse.</param>
        /// <returns><see cref="IProjection"/> instance.</returns>
        public IProjection Parse(string json)
        {
            var definition = JsonConvert.DeserializeObject<ProjectionDefinition>(json,
                new PropertyJsonConverter(),
                new PropertyExpressionDictionaryJsonConverter(),
                new PropertyChildrenDefinitionDictionaryJsonConverter(),
                new ConceptAsJsonConverter(),
                new ConceptAsDictionaryJsonConverter())!;

            return CreateFrom(definition);
        }

        /// <summary>
        /// Create a <see cref="IProjection"/> from <see cref="ProjectionDefinition"/>.
        /// </summary>
        /// <param name="definition"><see cref="ProjectionDefinition"/> to create from.</param>
        /// <returns><see cref="IProjection"/> instance.</returns>
        public IProjection CreateFrom(ProjectionDefinition definition) =>
            CreateProjectionFrom(
                definition,
                Property.Root,
                Property.Root,
                $"Root({definition.Identifier})",
                definition.Model,
                definition.From,
                new Dictionary<Property, ChildrenDefinition>(),
                _ => { });

        IProjection CreateProjectionFrom(
            ProjectionDefinition projectionDefinition,
            Property childrenAccessorProperty,
            Property identifiedByProperty,
            ProjectionPath path,
            ModelDefinition modelDefinition,
            IDictionary<EventType, FromDefinition> fromDefinitions,
            IDictionary<Property, ChildrenDefinition> childrenDefinitions,
            Action<IEnumerable<EventTypeWithKeyResolver>> addChildEventTypes)
        {
            var eventsForProjection = fromDefinitions.Select(kvp => new EventTypeWithKeyResolver(kvp.Key, string.IsNullOrEmpty(kvp.Value.ParentKey) ?
                    EventValueProviders.FromEventSourceId :
                    EventValueProviders.FromEventContent(kvp.Value.ParentKey!))).ToList();

            var childProjections = projectionDefinition.Children.Select(kvp => CreateProjectionFrom(
                    kvp.Value,
                    kvp.Key,
                    kvp.Value.IdentifiedBy,
                    $"{path} -> ChildrenAt({kvp.Key.Path})",
                    kvp.Value.Model,
                    kvp.Value.From,
                    projectionDefinition.Children,
                    _ =>
                    {
                        eventsForProjection.AddRange(_);
                        addChildEventTypes(_);
                    })).ToArray();

            var model = new Model(modelDefinition.Name, JSchema.Parse(modelDefinition.Schema));
            addChildEventTypes(eventsForProjection);

            var projection = new Projection(projectionDefinition.Identifier, path, model, eventsForProjection, childProjections);

            foreach (var (childrenProperty, childrenDefinition) in childrenDefinitions)
            {
                foreach (var (eventType, fromDefinition) in childrenDefinition.From)
                {
                    var propertyMappers = fromDefinition.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(kvp.Key, kvp.Value));
                    projection.Event.From(eventType).Child(childrenProperty, childrenDefinition.IdentifiedBy, EventValueProviders.FromEventSourceId, propertyMappers);
                }
            }

            foreach (var (eventType, fromDefinition) in fromDefinitions)
            {
                var propertyMappers = fromDefinition.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(kvp.Key, kvp.Value));
                projection.Event.From(eventType).Project(childrenAccessorProperty, identifiedByProperty,  EventValueProviders.FromEventSourceId, propertyMappers);
            }

            return projection;
        }
    }
}
