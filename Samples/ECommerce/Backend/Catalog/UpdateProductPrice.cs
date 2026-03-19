// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Catalog;

/// <summary>
/// Command for updating the price of an existing product.
/// </summary>
/// <param name="ProductId">The identifier of the product to update.</param>
/// <param name="NewPrice">The new price for the product.</param>
/// <param name="EffectiveFrom">The date and time from which the new price is effective.</param>
[Command]
[BelongsTo("CatalogService")]
public record UpdateProductPrice(ProductId ProductId, ProductPrice NewPrice, DateTimeOffset EffectiveFrom)
{
    /// <summary>
    /// Handles the command by printing the price update.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal Task Handle()
    {
        Console.WriteLine($"[Catalog] Updating price of product {ProductId} to {NewPrice} from {EffectiveFrom:O}");
        return Task.CompletedTask;
    }
}
