// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Shared.Projections.Definitions;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IFromBuilder{TModel, TEvent}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of parent builder.</typeparam>
public class FromBuilder<TModel, TEvent, TParentBuilder> : ModelPropertiesBuilder<TModel, TEvent, IFromBuilder<TModel, TEvent>, TParentBuilder>, IFromBuilder<TModel, TEvent>
    where TParentBuilder : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FromBuilder{TModel, TEvent, TParentBuilder}"/>.
    /// </summary>
    /// <param name="projectionBuilder">The parent <see cref="IProjectionBuilderFor{TModel}"/>.</param>
    public FromBuilder(IProjectionBuilder<TModel, TParentBuilder> projectionBuilder) : base(projectionBuilder)
    {
    }

    /// <inheritdoc/>
    public FromDefinition Build() => new(
        Properties: _propertyExpressions.ToDictionary(_ => _.TargetProperty, _ => _.Build()),
        Key: _key.Build(),
        ParentKey: _parentKey.Build());
}
