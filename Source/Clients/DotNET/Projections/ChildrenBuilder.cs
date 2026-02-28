// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Engine.Expressions;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IChildrenBuilder{TParentReadModel, TChildReadModel}"/>.
/// </summary>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for providing event type information.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for any JSON serialization.</param>
/// <param name="autoMap">AutoMap behavior for properties - inherits from parent by default.</param>
/// <typeparam name="TParentReadModel">Parent read model type.</typeparam>
/// <typeparam name="TChildReadModel">Child read model type.</typeparam>
public class ChildrenBuilder<TParentReadModel, TChildReadModel>(
    INamingPolicy namingPolicy,
    IEventTypes eventTypes,
    JsonSerializerOptions jsonSerializerOptions,
    AutoMap autoMap) :
    ProjectionBuilder<TChildReadModel, IChildrenBuilder<TParentReadModel, TChildReadModel>>(namingPolicy, eventTypes, jsonSerializerOptions, autoMap),
    IChildrenBuilder<TParentReadModel, TChildReadModel>
{
    readonly INamingPolicy _namingPolicy = namingPolicy;
    PropertyPath _identifiedBy = PropertyPath.NotSet;

#pragma warning disable IDE0052 // Remove unread private members
    // TODO: This is not used, but it should be - figure out what the purpose was. The FromEventProperty method is called from ReadModelPropertiesBuilder
    EventType? _fromEventPropertyEventType;
    IEventValueExpression? _fromEventPropertyExpression;

#pragma warning restore IDE0052 // Remove unread private members

    /// <inheritdoc/>
    public bool HasIdentifiedBy => _identifiedBy.IsSet;

    /// <inheritdoc/>
    public PropertyPath GetIdentifiedBy() => _identifiedBy;

    /// <inheritdoc/>
    public IChildrenBuilder<TParentReadModel, TChildReadModel> IdentifiedBy(PropertyPath propertyPath)
    {
        _identifiedBy = _namingPolicy.GetPropertyName(propertyPath);
        return this;
    }

    /// <inheritdoc/>
    public IChildrenBuilder<TParentReadModel, TChildReadModel> IdentifiedBy<TProperty>(Expression<Func<TChildReadModel, TProperty>> propertyExpression)
    {
        _identifiedBy = _namingPolicy.GetPropertyName(propertyExpression.GetPropertyPath());
        return this;
    }

    /// <inheritdoc/>
    public IChildrenBuilder<TParentReadModel, TChildReadModel> FromEventProperty<TEvent>(Expression<Func<TEvent, TChildReadModel>> propertyExpression)
    {
        _fromEventPropertyEventType = typeof(TEvent).GetEventType().ToContract();
        _fromEventPropertyExpression = new EventContentPropertyExpression(propertyExpression.GetPropertyPath());
        return this;
    }

    /// <summary>
    /// Build the <see cref="ChildrenDefinition"/>.
    /// </summary>
    /// <returns>A a new <see cref="ChildrenDefinition"/>.</returns>
    internal ChildrenDefinition Build()
    {
        return new()
        {
            IdentifiedBy = _identifiedBy,
            From = _fromDefinitions,
            Join = _joinDefinitions,
            Children = _childrenDefinitions.ToDictionary(_ => (string)_.Key, _ => _.Value),
            All = _fromEveryDefinition,
            RemovedWith = _removedWithDefinitions,
            RemovedWithJoin = _removedWithJoinDefinitions
        };
    }
}
