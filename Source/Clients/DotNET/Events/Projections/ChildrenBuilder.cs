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
    /// Represents an implementation of <see cref="IChildrenBuilder{TModel, TChildModel}"/>.
    /// </summary>
    /// <typeparam name="TParentModel">Parent model type.</typeparam>
    /// <typeparam name="TChildModel">Child model type.</typeparam>
    public class ChildrenBuilder<TParentModel, TChildModel> : IChildrenBuilder<TParentModel, TChildModel>
    {
        static readonly JSchemaGenerator _generator;
        readonly IEventTypes _eventTypes;
        readonly Dictionary<string, FromDefinition> _fromDefintions = new();
        readonly string _modelName;
        string _identifiedBy = string.Empty;
        string _removedWithEvent = string.Empty;

        static ChildrenBuilder()
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
        /// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
        public ChildrenBuilder(IEventTypes eventTypes)
        {
            _eventTypes = eventTypes;
            _modelName = typeof(TChildModel).Name.Pluralize().ToCamelCase();
        }

        /// <inheritdoc/>
        public IChildrenBuilder<TParentModel, TChildModel> From<TEvent>(Action<IFromBuilder<TChildModel, TEvent>> builderCallback)
        {
            var builder = new FromBuilder<TChildModel, TEvent>();
            builderCallback(builder);
            var eventType = _eventTypes.GetEventTypeIdFor(typeof(TEvent));
            _fromDefintions[eventType.ToString()] = builder.Build();
            return this;
        }

        /// <inheritdoc/>
        public IChildrenBuilder<TParentModel, TChildModel> IdentifiedBy<TProperty>(Expression<Func<TChildModel, TProperty>> propertyExpression)
        {
            _identifiedBy = propertyExpression.GetPropertyInfo().Name.ToCamelCase();
            return this;
        }

        /// <inheritdoc/>
        public IChildrenBuilder<TParentModel, TChildModel> RemovedWith<TEvent>()
        {
            _removedWithEvent = _eventTypes.GetEventTypeIdFor(typeof(TEvent)).ToString();
            return this;
        }

        /// <inheritdoc/>
        public ChildrenDefinition Build()
        {
            return new ChildrenDefinition(
                _identifiedBy,
                new ModelDefinition(_modelName, _generator.Generate(typeof(TChildModel)).ToString()),
                _fromDefintions,
                string.IsNullOrEmpty(_removedWithEvent) ? default : new RemovedWithDefinition(_removedWithEvent)
            );
        }
    }
}
