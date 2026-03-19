// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ECommerce.Catalog;

namespace ECommerce.Cart;

/// <summary>
/// Command for removing an item from a shopping cart.
/// </summary>
/// <param name="CartId">The identifier of the cart.</param>
/// <param name="ProductId">The identifier of the product to remove.</param>
[Command]
[BelongsTo("CartService")]
public record RemoveItemFromCart(CartId CartId, ProductId ProductId)
{
    /// <summary>
    /// Handles the command by printing the removal.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal Task Handle()
    {
        Console.WriteLine($"[Cart] Removing product {ProductId} from cart {CartId}");
        return Task.CompletedTask;
    }
}
