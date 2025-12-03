// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IRemovedWithBuilder{TReadModel, TEvent, TBuilder}"/>.
/// </summary>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for property names.</param>
public class RemovedWithJoinBuilder<TReadModel, TEvent>(INamingPolicy namingPolicy)
    : KeyBuilder<TEvent, RemovedWithJoinBuilder<TReadModel, TEvent>>(namingPolicy), IRemovedWithJoinBuilder<TReadModel, TEvent, RemovedWithJoinBuilder<TReadModel, TEvent>>
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
