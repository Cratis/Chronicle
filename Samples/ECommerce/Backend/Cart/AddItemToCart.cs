// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ECommerce.Catalog;

namespace ECommerce.Cart;

/// <summary>
/// Command for adding an item to a shopping cart.
/// </summary>
/// <param name="CartId">The identifier of the cart.</param>
/// <param name="ProductId">The identifier of the product to add.</param>
/// <param name="Quantity">The quantity of the product to add.</param>
[Command]
[BelongsTo("CartService")]
public record AddItemToCart(CartId CartId, ProductId ProductId, int Quantity)
{
    /// <summary>
    /// Handles the command by printing the cart addition.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal Task Handle()
    {
        Console.WriteLine($"[Cart] Adding {Quantity}x product {ProductId} to cart {CartId}");
        return Task.CompletedTask;
    }
}
