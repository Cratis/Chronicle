// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="IFromBuilder{TModel, TEvent}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of parent builder.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="FromBuilder{TModel, TEvent, TParentBuilder}"/>.
/// </remarks>
/// <param name="projectionBuilder">The parent <see cref="IProjectionBuilderFor{TModel}"/>.</param>
public class FromBuilder<TModel, TEvent, TParentBuilder>(IProjectionBuilder<TModel, TParentBuilder> projectionBuilder)
    : ModelPropertiesBuilder<TModel, TEvent, IFromBuilder<TModel, TEvent>, TParentBuilder>(projectionBuilder), IFromBuilder<TModel, TEvent>
        where TParentBuilder : class
{
    /// <inheritdoc/>
    public FromDefinition Build() => new()
    {
        Properties = _propertyExpressions.ToDictionary(_ => (string)_.TargetProperty, _ => _.Build()),
        Key = _key.Build(),
        ParentKey = _parentKey.Build()
    };
}
