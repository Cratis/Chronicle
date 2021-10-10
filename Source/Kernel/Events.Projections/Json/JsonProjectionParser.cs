// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Newtonsoft.Json;

namespace Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents a parser for JSON definition of a <see cref="IProjection"/>.
    /// </summary>
    public class JsonProjectionParser
    {
        /// <summary>
        /// Parse a JSON string definition and produce a <see cref="IProjection"/>.
        /// </summary>
        /// <param name="json">JSON to parse.</param>
        /// <returns><see cref="IProjection"/> instance.</returns>
        public IProjection Parse(string json)
        {
            var eventsForProjection = new List<EventTypeWithKeyResolver>();
            var definition = JsonConvert.DeserializeObject<ProjectionDefinition>(json,
                new ConceptAsJsonConverter(),
                new ConceptAsDictionaryJsonConverter())!;

            eventsForProjection.AddRange(definition.From.Keys.Select(_ => new EventTypeWithKeyResolver(_, KeyResolvers.EventSourceId)));

            var model = new Model(definition.Model.Name);

            var projection = new Projection(definition.Identifier, model, eventsForProjection);
            foreach (var (eventType, definitions) in definition.From)
            {
                var propertyMappers = new List<PropertyMapper>();
                foreach (var (target, source) in definitions)
                {
                    propertyMappers.Add(PropertyMappers.FromEventContent(source, target));
                }

                projection.Event.From(eventType).Project(propertyMappers);
            }

            return projection;
        }
    }
}
