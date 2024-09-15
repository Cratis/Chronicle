// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IRemovedWithBuilder{TModel, TEvent, TBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public class RemovedWithBuilder<TModel, TEvent> : KeyAndParentKeyBuilder<TEvent, RemovedWithBuilder<TModel, TEvent>>, IRemovedWithBuilder<TModel, TEvent, RemovedWithBuilder<TModel, TEvent>>
{
    /// <inheritdoc/>
    public RemovedWithDefinition Build() => new()
    {
        Key = _keyExpression,
        ParentKey = _parentKeyExpression
    };
}
