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
            var eventsForProjection = definition.From.Keys.Select(_ => new EventTypeWithKeyResolver(_, KeyResolvers.EventSourceId)).ToArray();
            var model = new Model(definition.Model.Name, JSchema.Parse(definition.Model.Schema));

            var projection = new Projection(definition.Identifier, model, eventsForProjection);
            foreach (var (eventType, definitions) in definition.From)
            {
                var propertyMappers = definitions.Select(kvp => _propertyMapperExpressionResolvers.Resolve(kvp.Key, kvp.Value));
                projection.Event.From(eventType).Project(propertyMappers);
            }

            return projection;
        }
    }
}
