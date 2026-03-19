// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Catalog;

/// <summary>
/// Represents the name of a product.
/// </summary>
/// <param name="Value">The underlying string value.</param>
public record ProductName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Gets a sentinel value representing no product name.
    /// </summary>
    public static readonly ProductName NotSet = new(string.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="ProductName"/>.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator ProductName(string value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="ProductName"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="name">The product name.</param>
    public static implicit operator string(ProductName name) => name.Value;
}
