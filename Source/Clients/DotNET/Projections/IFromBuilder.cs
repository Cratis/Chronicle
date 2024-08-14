// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for building from expressions.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public interface IFromBuilder<TModel, TEvent> : IModelPropertiesBuilder<TModel, TEvent, IFromBuilder<TModel, TEvent>>
{
    /// <summary>
    /// Automatically map event properties to model properties on the events added.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    IFromBuilder<TModel, TEvent> AutoMap();

    /// <summary>
    /// Build <see cref="FromDefinition"/> from the builder.
    /// </summary>
    /// <returns>A new instance of <see cref="FromDefinition"/>.</returns>
    FromDefinition Build();
}
