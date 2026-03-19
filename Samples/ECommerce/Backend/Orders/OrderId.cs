// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Orders;

/// <summary>
/// Represents the unique identifier of an order.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record OrderId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets a sentinel value representing no order id.
    /// </summary>
    public static readonly OrderId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to an <see cref="OrderId"/>.
    /// </summary>
    /// <param name="value">The Guid value.</param>
    public static implicit operator OrderId(Guid value) => new(value);

    /// <summary>
    /// Implicitly converts an <see cref="OrderId"/> to a <see cref="Guid"/>.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    public static implicit operator Guid(OrderId id) => id.Value;

    /// <summary>
    /// Creates a new order identifier.
    /// </summary>
    /// <returns>A new <see cref="OrderId"/>.</returns>
    public static OrderId New() => new(Guid.NewGuid());
}
