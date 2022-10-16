// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Defines the builder for building from expressions.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public interface IFromBuilder<TModel, TEvent> : IModelPropertiesBuilder<TModel, TEvent, IFromBuilder<TModel, TEvent>>
{
    /// <summary>
    /// Build <see cref="FromDefinition"/> from the builder.
    /// </summary>
    /// <returns>A new instance of <see cref="FromDefinition"/>.</returns>
    FromDefinition Build();
}
