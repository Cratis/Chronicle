// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IFromBuilder{TModel, TEvent}"/>.
/// </summary>
/// <param name="projectionBuilder">The parent <see cref="IProjectionBuilderFor{TModel}"/>.</param>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of parent builder.</typeparam>
public class FromBuilder<TModel, TEvent, TParentBuilder>(IProjectionBuilder<TModel, TParentBuilder> projectionBuilder)
    : ModelPropertiesBuilder<TModel, TEvent, IFromBuilder<TModel, TEvent>, TParentBuilder>(projectionBuilder), IFromBuilder<TModel, TEvent>
        where TParentBuilder : class
{
    /// <summary>
    /// Build <see cref="FromDefinition"/> from the builder.
    /// </summary>
    /// <returns>A new instance of <see cref="FromDefinition"/>.</returns>
    internal FromDefinition Build() => new()
    {
        Properties = _propertyExpressions.ToDictionary(_ => (string)_.Key, _ => _.Value.Build()),
        Key = _keyExpression,
        ParentKey = _parentKeyExpression
    };
}
