// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Cart;

/// <summary>
/// Represents the unique identifier of a customer.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record CustomerId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets a sentinel value representing no customer id.
    /// </summary>
    public static readonly CustomerId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="CustomerId"/>.
    /// </summary>
    /// <param name="value">The Guid value.</param>
    public static implicit operator CustomerId(Guid value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="CustomerId"/> to a <see cref="Guid"/>.
    /// </summary>
    /// <param name="id">The customer identifier.</param>
    public static implicit operator Guid(CustomerId id) => id.Value;

    /// <summary>
    /// Creates a new customer identifier.
    /// </summary>
    /// <returns>A new <see cref="CustomerId"/>.</returns>
    public static CustomerId New() => new(Guid.NewGuid());
}
