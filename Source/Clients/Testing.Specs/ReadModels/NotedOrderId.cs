// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Identifier for a <see cref="NotedOrder"/>.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record NotedOrderId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>
    /// Creates a new <see cref="NotedOrderId"/>.
    /// </summary>
    /// <returns>A new <see cref="NotedOrderId"/>.</returns>
    public static NotedOrderId New() => new(Guid.NewGuid());

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="NotedOrderId"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> to convert.</param>
    public static implicit operator NotedOrderId(Guid value) => new(value);
}
