// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Expressions;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IChildrenBuilder{TModel, TChildModel}"/>.
/// </summary>
/// <typeparam name="TParentModel">Parent model type.</typeparam>
/// <typeparam name="TChildModel">Child model type.</typeparam>
/// <remarks>
/// /// Initializes a new instance of the <see cref="ProjectionBuilderFor{TModel}"/> class.
/// </remarks>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
/// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
public class ChildrenBuilder<TParentModel, TChildModel>(
    IEventTypes eventTypes,
    IJsonSchemaGenerator schemaGenerator,
    JsonSerializerOptions jsonSerializerOptions) :
    ProjectionBuilder<TChildModel, IChildrenBuilder<TParentModel, TChildModel>>(eventTypes, schemaGenerator, jsonSerializerOptions),
    IChildrenBuilder<TParentModel, TChildModel>
{
    PropertyPath _identifiedBy = PropertyPath.NotSet;
    EventType? _fromEventPropertyEventType;
    IEventValueExpression? _fromEventPropertyExpression;

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
                Schema = schemaGenerator.Generate(typeof(TChildModel)).ToJson()
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
