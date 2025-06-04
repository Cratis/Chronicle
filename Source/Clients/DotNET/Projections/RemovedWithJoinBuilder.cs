// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IRemovedWithBuilder{TModel, TEvent, TBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public class RemovedWithJoinBuilder<TModel, TEvent> : KeyBuilder<TEvent, RemovedWithJoinBuilder<TModel, TEvent>>, IRemovedWithJoinBuilder<TModel, TEvent, RemovedWithJoinBuilder<TModel, TEvent>>
{
    /// <summary>
    /// Build the removed with join definition.
    /// </summary>
    /// <returns>A new <see cref="RemovedWithJoinDefinition"/>.</returns>
    internal RemovedWithJoinDefinition Build() => new()
    {
        Key = _keyExpression,
    };
}
