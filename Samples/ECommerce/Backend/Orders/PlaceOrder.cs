// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ECommerce.Cart;

namespace ECommerce.Orders;

/// <summary>
/// Command for placing an order from a shopping cart.
/// </summary>
/// <param name="OrderId">The identifier for the new order.</param>
/// <param name="CustomerId">The identifier of the customer placing the order.</param>
/// <param name="CartId">The identifier of the cart being converted to an order.</param>
/// <param name="OrderedAt">The date and time when the order was placed.</param>
/// <param name="ShippingAddress">The shipping address for the order.</param>
[Command]
[BelongsTo("OrderService")]
public record PlaceOrder(
    OrderId OrderId,
    CustomerId CustomerId,
    CartId CartId,
    DateTimeOffset OrderedAt,
    string ShippingAddress)
{
    /// <summary>
    /// Handles the command by printing the order placement.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal Task Handle()
    {
        Console.WriteLine($"[Orders] Placing order {OrderId} for customer {CustomerId}");
        Console.WriteLine($"[Orders] Cart: {CartId}, Ordered at: {OrderedAt:O}, Shipping to: {ShippingAddress}");
        return Task.CompletedTask;
    }
}
