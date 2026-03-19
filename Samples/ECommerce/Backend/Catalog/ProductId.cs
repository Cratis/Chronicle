// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Catalog;

/// <summary>
/// Represents the unique identifier of a product.
/// </summary>
/// <param name="Value">The underlying Guid value.</param>
public record ProductId(Guid Value) : ConceptAs<Guid>(Value)
{
    /// <summary>
    /// Gets a sentinel value representing no product id.
    /// </summary>
    public static readonly ProductId NotSet = new(Guid.Empty);

    /// <summary>
    /// Implicitly converts a <see cref="Guid"/> to a <see cref="ProductId"/>.
    /// </summary>
    /// <param name="value">The Guid value.</param>
    public static implicit operator ProductId(Guid value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="ProductId"/> to a <see cref="Guid"/>.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    public static implicit operator Guid(ProductId id) => id.Value;

    /// <summary>
    /// Creates a new product identifier.
    /// </summary>
    /// <returns>A new <see cref="ProductId"/>.</returns>
    public static ProductId New() => new(Guid.NewGuid());
}
