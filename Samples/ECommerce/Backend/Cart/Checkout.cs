// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ECommerce.Orders;

namespace ECommerce.Cart;

/// <summary>
/// Command for checking out a shopping cart and creating an order.
/// Uses <see cref="OneOf{T0,T1}"/> to return either a successful result or an error.
/// </summary>
/// <param name="CartId">The identifier of the cart to checkout.</param>
/// <param name="CustomerId">The identifier of the customer.</param>
[Command]
[BelongsTo("CartService")]
public record Checkout(CartId CartId, CustomerId CustomerId)
{
    /// <summary>
    /// Handles the command and returns either a checkout result or an error.
    /// </summary>
    /// <returns>A <see cref="OneOf{T0,T1}"/> containing the result or an error.</returns>
    internal Task<OneOf<CheckoutResult, CheckoutError>> Handle()
    {
        Console.WriteLine($"[Cart] Checking out cart {CartId} for customer {CustomerId}");

        // Simulate checkout logic
        if (CartId == CartId.NotSet)
        {
            return Task.FromResult<OneOf<CheckoutResult, CheckoutError>>(CheckoutError.EmptyCart);
        }

        var orderId = OrderId.New();
        Console.WriteLine($"[Cart] Checkout successful, created order {orderId}");
        return Task.FromResult<OneOf<CheckoutResult, CheckoutError>>(new CheckoutResult(orderId));
    }
}
