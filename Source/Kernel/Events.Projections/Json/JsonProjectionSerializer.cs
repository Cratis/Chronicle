// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Cratis.Events.Projections.Definitions;
using Cratis.Events.Projections.Expressions;
using Cratis.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents a parser for JSON definition of a <see cref="IProjection"/>.
    /// </summary>
    public class JsonProjectionSerializer
    {
        readonly IPropertyMapperExpressionResolvers _propertyMapperExpressionResolvers;
        readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonProjectionSerializer"/>.
        /// </summary>
        /// <param name="propertyMapperExpressionResolvers"><see cref="IPropertyMapperExpressionResolvers"/> for resolving event value expressions.</param>
        public JsonProjectionSerializer(IPropertyMapperExpressionResolvers propertyMapperExpressionResolvers)
        {
            _propertyMapperExpressionResolvers = propertyMapperExpressionResolvers;

            _serializer = new JsonSerializer();
            _serializer.Converters.Add(new PropertyPathJsonConverter());
            _serializer.Converters.Add(new PropertyExpressionDictionaryJsonConverter());
            _serializer.Converters.Add(new PropertyPathChildrenDefinitionDictionaryJsonConverter());
            _serializer.Converters.Add(new ConceptAsJsonConverter());
            _serializer.Converters.Add(new ConceptAsDictionaryJsonConverter());
        }

        /// <summary>
        /// Serialize a <see cref="ProjectionDefinition"/>.
        /// </summary>
        /// <param name="definition"><see cref="ProjectionDefinition"/> to serialize.</param>
        /// <returns>JSON representation.</returns>
        public string Serialize(ProjectionDefinition definition)
        {
            var writer = new StringWriter();
            _serializer.Serialize(writer, definition);
            return writer.ToString();
        }

        /// <summary>
        /// Deserialize a JSON string definition into <see cref="ProjectionDefinition"/>.
        /// </summary>
        /// <param name="json">JSON to parse.</param>
        /// <returns><see cref="ProjectionDefinition"/> instance.</returns>
        public ProjectionDefinition Deserialize(string json) => _serializer.Deserialize<ProjectionDefinition>(new JsonTextReader(new StringReader(json)))!;

        /// <summary>
        /// Create a <see cref="IProjection"/> from <see cref="ProjectionDefinition"/>.
        /// </summary>
        /// <param name="definition"><see cref="ProjectionDefinition"/> to create from.</param>
        /// <returns><see cref="IProjection"/> instance.</returns>
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

            var projection = new Projection(projectionDefinition.Identifier, path, model, eventsForProjection, childProjections);

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
