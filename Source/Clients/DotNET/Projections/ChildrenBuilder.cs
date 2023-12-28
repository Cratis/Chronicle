// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Contracts.Projections;
using Aksio.Cratis.Projections.Expressions;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Reflection;
using Aksio.Cratis.Schemas;
using EventType = Aksio.Cratis.Kernel.Contracts.Events.EventType;

namespace Aksio.Cratis.Projections;

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
        _fromEventPropertyEventType = typeof(TEvent).GetEventType().ToContract();
        _fromEventPropertyExpression = new EventContentPropertyExpression(propertyExpression.GetPropertyPath());
        return this;
    }

    /// <inheritdoc/>
    public ChildrenDefinition Build()
    {
        return new()
        {
            IdentifiedBy = _identifiedBy,
            Model = new()
            {
                Name = _modelName,
                Schema = _schemaGenerator.Generate(typeof(TChildModel)).ToJson()
            },
            InitialModelState = _initialValues.ToJsonString(),
            From = _fromDefinitions,
            Join = _joinDefinitions,
            Children = _childrenDefinitions.ToDictionary(_ => (string)_.Key, _ => _.Value),
            All = _allDefinition,
            RemovedWith = _removedWithEvent == default ? default : new() { Event = _removedWithEvent }
        };
    }
}
