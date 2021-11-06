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
                Property.Root,
                Property.Root,
                definition.Identifier,
                $"Root({definition.Identifier})",
                definition.Model,
                definition.From,
                new Dictionary<Property, ChildrenDefinition>(),
                definition.Children,
                _ => { },
                (initialState, _, __) => initialState);

        IProjection CreateProjectionFrom(
            Property childrenAccessorProperty,
            Property identifiedByProperty,
            ProjectionId identifier,
            ProjectionPath path,
            ModelDefinition modelDefinition,
            IDictionary<EventType, FromDefinition> fromDefinitions,
            IDictionary<Property, ChildrenDefinition> parentChildrenDefinitions,
            IDictionary<Property, ChildrenDefinition> childrenDefinitions,
            Action<IEnumerable<EventTypeWithKeyResolver>> addChildEventTypes,
            InstanceAccessor instanceAccessor)
        {
            var eventsForProjection = fromDefinitions.Select(kvp => new EventTypeWithKeyResolver(kvp.Key, string.IsNullOrEmpty(kvp.Value.ParentKey) ?
                    EventValueProviders.FromEventSourceId :
                    EventValueProviders.FromEventContent(kvp.Value.ParentKey!))).ToList();

            var childProjections = childrenDefinitions.Select(kvp => CreateProjectionFrom(
                    kvp.Key,
                    kvp.Value.IdentifiedBy,
                    Guid.Empty,
                    $"{path} -> ChildrenAt({kvp.Key.Path})",
                    kvp.Value.Model,
                    kvp.Value.From,
                    childrenDefinitions,
                    new Dictionary<Property, ChildrenDefinition>(),
                    _ =>
                    {
                        eventsForProjection.AddRange(_);
                        addChildEventTypes(_);
                    },
                    (initialState, @event, keyResolver) =>
                    {
                        var children = kvp.Key.GetValue(initialState) as IEnumerable<ExpandoObject>;
                        var key = keyResolver(@event);
                        return children!.FindByKey(kvp.Value.IdentifiedBy, key)!;
                    })).ToArray();

            var model = new Model(modelDefinition.Name, JSchema.Parse(modelDefinition.Schema));
            addChildEventTypes(eventsForProjection);

            var projection = new Projection(identifier, path, model, eventsForProjection, childProjections);

            foreach (var (childrenProperty, childrenDefinition) in parentChildrenDefinitions)
            {
                foreach (var (eventType, fromDefinition) in childrenDefinition.From)
                {
                    projection.Event.From(eventType).Child(childrenProperty, childrenDefinition.IdentifiedBy, EventValueProviders.FromEventSourceId);
                }
            }

            foreach (var (eventType, fromDefinition) in fromDefinitions)
            {
                var propertyMappers = fromDefinition.Properties.Select(kvp => _propertyMapperExpressionResolvers.Resolve(kvp.Key, kvp.Value));
                projection.Event.From(eventType).Project(childrenAccessorProperty, identifiedByProperty, instanceAccessor, EventValueProviders.FromEventSourceId, propertyMappers);
            }

            return projection;
        }
    }
}
