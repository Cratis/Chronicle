// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Linq.Expressions;
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

            var eventParameter = Expression.Parameter(typeof(Event));
            var targetParameter = Expression.Parameter(typeof(ExpandoObject));
            var contentAccessor = Expression.Property(eventParameter, typeof(Event), "Content");
            var itemProperty = typeof(IDictionary<string, object>).GetProperty("Item")!;

            var model = new Model(definition.Model.Name);

            var projection = new Projection(definition.Identifier, model, eventsForProjection);
            foreach (var (eventType, definitions) in definition.From)
            {
                var propertyMappers = new List<PropertyMapper>();
                foreach (var (target, source) in definitions)
                {
                    var propertyMapperExpression = Expression.Assign(
                        Expression.Property(targetParameter, itemProperty, Expression.Constant(target)),
                        Expression.Property(
                            contentAccessor,
                            itemProperty,
                            Expression.Constant(source)));

                    propertyMappers.Add(Expression.Lambda<PropertyMapper>(propertyMapperExpression, new[] {
                        eventParameter,
                        targetParameter
                    }).Compile());
                }

                projection.Event.From(eventType).Project(propertyMappers);
            }

            return projection;
        }
    }
}
