// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Catalog;

/// <summary>
/// Command for adding a new product to the catalog.
/// </summary>
/// <param name="ProductId">The unique identifier for the product.</param>
/// <param name="Name">The name of the product.</param>
/// <param name="Price">The initial price of the product.</param>
/// <param name="Category">The category the product belongs to.</param>
[Command]
[BelongsTo("CatalogService")]
public record AddProduct(ProductId ProductId, ProductName Name, ProductPrice Price, string Category)
{
    /// <summary>
    /// Handles the command by printing product information.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal Task Handle()
    {
        Console.WriteLine($"[Catalog] Adding product: {Name} (ID: {ProductId}, Price: {Price}, Category: {Category})");
        return Task.CompletedTask;
    }
}
