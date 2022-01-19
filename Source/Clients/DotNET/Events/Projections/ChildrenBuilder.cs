// Copyright (c) Cratis. All rights reserved.
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
    /// Represents an implementation of <see cref="IChildrenBuilder{TModel, TChildModel}"/>.
    /// </summary>
    /// <typeparam name="TParentModel">Parent model type.</typeparam>
    /// <typeparam name="TChildModel">Child model type.</typeparam>
    public class ChildrenBuilder<TParentModel, TChildModel> : IChildrenBuilder<TParentModel, TChildModel>
    {
        readonly IEventTypes _eventTypes;
        readonly IJsonSchemaGenerator _schemaGenerator;
        readonly Dictionary<EventType, FromDefinition> _fromDefintions = new();
        readonly string _modelName;
        string _identifiedBy = string.Empty;
        string _removedWithEvent = string.Empty;

        /// <summary>
        /// /// Initializes a new instance of the <see cref="ProjectionBuilderFor{TModel}"/> class.
        /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
        /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
        public ChildrenBuilder(
            IEventTypes eventTypes,
            IJsonSchemaGenerator schemaGenerator)
        {
            _eventTypes = eventTypes;
            _schemaGenerator = schemaGenerator;
            _modelName = typeof(TChildModel).Name.Pluralize().ToCamelCase();
        }

        /// <inheritdoc/>
        public IChildrenBuilder<TParentModel, TChildModel> From<TEvent>(Action<IFromBuilder<TChildModel, TEvent>> builderCallback)
        {
            var builder = new FromBuilder<TChildModel, TEvent>();
            builderCallback(builder);
            var eventType = _eventTypes.GetEventTypeFor(typeof(TEvent));
            _fromDefintions[eventType] = builder.Build();
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
            _removedWithEvent = _eventTypes.GetEventTypeFor(typeof(TEvent)).ToString();
            return this;
        }

        /// <inheritdoc/>
        public ChildrenDefinition Build()
        {
            return new ChildrenDefinition(
                _identifiedBy,
                new ModelDefinition(_modelName, _schemaGenerator.Generate(typeof(TChildModel)).ToJson()),
                _fromDefintions,
                new Dictionary<PropertyPath, ChildrenDefinition>(),
                string.IsNullOrEmpty(_removedWithEvent) ? default : new RemovedWithDefinition(_removedWithEvent));
        }
    }
}
