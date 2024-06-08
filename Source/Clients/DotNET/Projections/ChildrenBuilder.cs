// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Text.Json;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Schemas;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Expressions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IChildrenBuilder{TModel, TChildModel}"/>.
/// </summary>
/// <typeparam name="TParentModel">Parent model type.</typeparam>
/// <typeparam name="TChildModel">Child model type.</typeparam>
public class ChildrenBuilder<TParentModel, TChildModel> :
    ProjectionBuilder<TChildModel,
    IChildrenBuilder<TParentModel, TChildModel>>,
    IChildrenBuilder<TParentModel, TChildModel>
{
    PropertyPath _identifiedBy = PropertyPath.NotSet;
    EventType? _fromEventPropertyEventType;
    IEventValueExpression? _fromEventPropertyExpression;

    /// <summary>
    /// /// Initializes a new instance of the <see cref="ProjectionBuilderFor{TModel}"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
    public ChildrenBuilder(
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        JsonSerializerOptions jsonSerializerOptions)
        : base(eventTypes, schemaGenerator, jsonSerializerOptions)
    {
    }

    /// <inheritdoc/>
    public IChildrenBuilder<TParentModel, TChildModel> IdentifiedBy(PropertyPath propertyPath)
    {
        _identifiedBy = propertyPath;
        return this;
    }

    /// <inheritdoc/>
    public IChildrenBuilder<TParentModel, TChildModel> IdentifiedBy<TProperty>(Expression<Func<TChildModel, TProperty>> propertyExpression)
    {
        _identifiedBy = propertyExpression.GetPropertyPath();
        return this;
    }

    /// <inheritdoc/>
    public IChildrenBuilder<TParentModel, TChildModel> FromEventProperty<TEvent>(Expression<Func<TEvent, TChildModel>> propertyExpression)
    {
        _fromEventPropertyEventType = typeof(TEvent).GetEventType();
        _fromEventPropertyExpression = new EventContentPropertyExpression(propertyExpression.GetPropertyPath());
        return this;
    }

    /// <inheritdoc/>
    public ChildrenDefinition Build()
    {
        FromEventPropertyDefinition? fromEventPropertyDefinition = null;
        if (_fromEventPropertyEventType is not null && _fromEventPropertyExpression is not null)
        {
            fromEventPropertyDefinition = new FromEventPropertyDefinition(_fromEventPropertyEventType, _fromEventPropertyExpression.Build());
        }

        return new ChildrenDefinition(
            _identifiedBy,
            new ModelDefinition(_modelName, _schemaGenerator.Generate(typeof(TChildModel)).ToJson()),
            _initialValues,
            _fromDefinitions,
            _joinDefinitions,
            _childrenDefinitions,
            _allDefinition,
            fromEventPropertyDefinition,
            _removedWithEvent == default ? default : new RemovedWithDefinition(_removedWithEvent));
    }
}
