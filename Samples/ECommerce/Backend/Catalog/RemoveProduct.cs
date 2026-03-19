// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Catalog;

/// <summary>
/// Command for removing a product from the catalog.
/// </summary>
/// <param name="ProductId">The identifier of the product to remove.</param>
[Command]
[BelongsTo("CatalogService")]
public record RemoveProduct(ProductId ProductId)
{
    /// <summary>
    /// Handles the command by printing the removal.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal Task Handle()
    {
        Console.WriteLine($"[Catalog] Removing product: {ProductId}");
        return Task.CompletedTask;
    }
}
