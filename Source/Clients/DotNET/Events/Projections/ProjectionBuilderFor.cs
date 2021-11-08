// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Reflection;
using Cratis.Strings;
using Humanizer;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

namespace Cratis.Events.Projections
{
    /// <summary>
    /// /// Represents an implementation of <see cref="IProjectionBuilderFor{TModel}"/>.
    /// </summary>
    /// <typeparam name="TModel">Type of model.</typeparam>
    public class ProjectionBuilderFor<TModel> : IProjectionBuilderFor<TModel>
    {
        static readonly JSchemaGenerator _generator;
        readonly ProjectionId _identifier;
        readonly IEventTypes _eventTypes;
        string _modelName;
        readonly Dictionary<string, FromDefinition> _fromDefintions = new();
        readonly Dictionary<string, ChildrenDefinition> _childrenDefinitions = new();

        static ProjectionBuilderFor()
        {
            _generator = new JSchemaGenerator
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            _generator.GenerationProviders.Add(new StringEnumGenerationProvider());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectionBuilderFor{TModel}"/> class.
        /// </summary>
        /// <param name="identifier">The unique identifier for the projection.</param>
        /// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
        public ProjectionBuilderFor(ProjectionId identifier, IEventTypes eventTypes)
        {
            _identifier = identifier;
            _eventTypes = eventTypes;
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
            var eventType = _eventTypes.GetEventTypeIdFor(typeof(TEvent));
            _fromDefintions[eventType.ToString()] = builder.Build();
            return this;
        }

        /// <inheritdoc/>
        public IProjectionBuilderFor<TModel> Children<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Action<IChildrenBuilder<TModel, TChildModel>> builderCallback)
        {
            var builder = new ChildrenBuilder<TModel, TChildModel>(_eventTypes);
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
                new ModelDefinition(_modelName, _generator.Generate(typeof(TModel)).ToString()),
                _fromDefintions,
                _childrenDefinitions);
        }
    }
}
