// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                new ConceptAsJsonConverter(),
                new ConceptAsDictionaryJsonConverter())!;

            return CreateFrom(definition);
        }

        /// <summary>
        /// Create a <see cref="IProjection"/> from <see cref="ProjectionDefinition"/>.
        /// </summary>
        /// <param name="definition"><see cref="ProjectionDefinition"/> to create from.</param>
        /// <returns><see cref="IProjection"/> instance.</returns>
        public IProjection CreateFrom(ProjectionDefinition definition)
        {
            return CreateProjectionFrom(definition.Identifier, definition.Model, definition.From, definition.Children, _ => { });
        }

        IProjection CreateProjectionFrom(
            ProjectionId identifier,
            ModelDefinition modelDefinition,
            IDictionary<EventType, FromDefinition> fromDefinitions,
            IDictionary<string, ChildrenDefinition> childrenDefinitions,
            Action<IEnumerable<EventTypeWithKeyResolver>> addChildEvents)
        {
            var eventsForProjection = fromDefinitions.Keys.Select(_ => new EventTypeWithKeyResolver(_, KeyResolvers.EventSourceId)).ToList();
            var childProjections = childrenDefinitions.Select(kvp => CreateProjectionFrom(
                    Guid.Empty,
                    kvp.Value.Model,
                    kvp.Value.From,
                    new Dictionary<string, ChildrenDefinition>(),
                    _ =>
                    {
                        eventsForProjection.AddRange(_);
                        addChildEvents(_);
                    }));

            var model = new Model(modelDefinition.Name, JSchema.Parse(modelDefinition.Schema));

            var projection = new Projection(identifier, model, eventsForProjection, childProjections);
            foreach (var (eventType, definitions) in fromDefinitions)
            {
                var propertyMappers = definitions.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(kvp.Key, kvp.Value));
                projection.Event.From(eventType).Project(propertyMappers);
            }

            foreach (var (property, definitions) in childrenDefinitions)
            {
                // Hook up child relationship extensions
            }

            return projection;
        }
    }
}
