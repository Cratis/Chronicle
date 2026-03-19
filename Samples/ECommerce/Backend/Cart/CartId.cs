// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Cart;

/// <summary>
/// Represents the unique identifier of a shopping cart.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record CartId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets a sentinel value representing no cart id.
    /// </summary>
    public static readonly CartId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="CartId"/>.
    /// </summary>
    /// <param name="value">The Guid value.</param>
    public static implicit operator CartId(Guid value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="CartId"/> to a <see cref="Guid"/>.
    /// </summary>
    /// <param name="id">The cart identifier.</param>
    public static implicit operator Guid(CartId id) => id.Value;

    /// <summary>
    /// Creates a new cart identifier.
    /// </summary>
    /// <returns>A new <see cref="CartId"/>.</returns>
    public static CartId New() => new(Guid.NewGuid());
}
