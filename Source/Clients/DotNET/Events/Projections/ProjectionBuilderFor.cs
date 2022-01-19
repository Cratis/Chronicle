// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Strings;
using Humanizer;

namespace Aksio.Cratis.Events.Projections
{
    /// <summary>
    /// /// Represents an implementation of <see cref="IProjectionBuilderFor{TModel}"/>.
    /// </summary>
    /// <typeparam name="TModel">Type of model.</typeparam>
    public class ProjectionBuilderFor<TModel> : IProjectionBuilderFor<TModel>
    {
        readonly ProjectionId _identifier;
        readonly IEventTypes _eventTypes;
        readonly IJsonSchemaGenerator _schemaGenerator;
        readonly Dictionary<EventType, FromDefinition> _fromDefintions = new();
        readonly Dictionary<PropertyPath, ChildrenDefinition> _childrenDefinitions = new();
        string _modelName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionBuilderFor{TModel}"/> class.
        /// </summary>
        /// <param name="identifier">The unique identifier for the projection.</param>
        /// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
        /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
        public ProjectionBuilderFor(
            ProjectionId identifier,
            IEventTypes eventTypes,
            IJsonSchemaGenerator schemaGenerator)
        {
            _identifier = identifier;
            _eventTypes = eventTypes;
            _schemaGenerator = schemaGenerator;
            _modelName = typeof(TModel).Name.Pluralize().ToCamelCase();
        }

        /// <inheritdoc/>
        public IProjectionBuilderFor<TModel> ModelName(string modelName)
        {
            _modelName = modelName;
            return this;
        }

        /// <inheritdoc/>
        public IProjectionBuilderFor<TModel> From<TEvent>(Action<IFromBuilder<TModel, TEvent>> builderCallback)
        {
            var builder = new FromBuilder<TModel, TEvent>();
            builderCallback(builder);
            var eventType = _eventTypes.GetEventTypeFor(typeof(TEvent));
            _fromDefintions[eventType] = builder.Build();
            return this;
        }

        /// <inheritdoc/>
        public IProjectionBuilderFor<TModel> Children<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Action<IChildrenBuilder<TModel, TChildModel>> builderCallback)
        {
            var builder = new ChildrenBuilder<TModel, TChildModel>(_eventTypes, _schemaGenerator);
            builderCallback(builder);
            _childrenDefinitions[targetProperty.GetPropertyInfo().Name.ToCamelCase()] = builder.Build();
            return this;
        }

        /// <inheritdoc/>
        public ProjectionDefinition Build()
        {
            return new ProjectionDefinition(
                _identifier,
                typeof(TModel).FullName ?? "[N/A]",
                new ModelDefinition(_modelName, _schemaGenerator.Generate(typeof(TModel)).ToJson()),
                _fromDefintions,
                _childrenDefinitions);
        }
    }
}
