// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IFromBuilder{TModel, TEvent}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public class FromBuilder<TModel, TEvent> : ModelPropertiesBuilder<TModel, TEvent, IFromBuilder<TModel, TEvent>>, IFromBuilder<TModel, TEvent>
{
    /// <inheritdoc/>
    public FromDefinition Build() => new(
        Properties: _propertyExpressions.ToDictionary(_ => _.TargetProperty, _ => _.Build()),
        Key: _key,
        ParentKey: _parentKey);
}
