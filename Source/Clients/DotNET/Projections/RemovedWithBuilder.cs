// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IRemovedWithBuilder{TReadModel, TEvent, TBuilder}"/>.
/// </summary>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public class RemovedWithBuilder<TReadModel, TEvent> : KeyAndParentKeyBuilder<TEvent, RemovedWithBuilder<TReadModel, TEvent>>, IRemovedWithBuilder<TReadModel, TEvent, RemovedWithBuilder<TReadModel, TEvent>>
{
    /// <summary>
    /// Build the removed with definition.
    /// </summary>
    /// <returns>A new <see cref="RemovedWithDefinition"/>.</returns>
    internal RemovedWithDefinition Build() => new()
    {
        Key = _keyExpression,
        ParentKey = _parentKeyExpression
    };
}
