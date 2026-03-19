// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Catalog;

/// <summary>
/// Represents the price of a product.
/// </summary>
/// <param name="Value">The underlying decimal value.</param>
public record ProductPrice(decimal Value) : ConceptAs<decimal>(Value)
{
    /// <summary>
    /// Gets a sentinel value representing no price.
    /// </summary>
    public static readonly ProductPrice NotSet = new(0m);

    /// <summary>
    /// Implicitly converts a <see cref="decimal"/> to a <see cref="ProductPrice"/>.
    /// </summary>
    /// <param name="value">The decimal value.</param>
    public static implicit operator ProductPrice(decimal value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="ProductPrice"/> to a <see cref="decimal"/>.
    /// </summary>
    /// <param name="price">The product price.</param>
    public static implicit operator decimal(ProductPrice price) => price.Value;
}
