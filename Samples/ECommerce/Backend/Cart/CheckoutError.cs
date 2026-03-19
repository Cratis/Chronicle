// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Cart;

/// <summary>
/// Represents an error that occurred during cart checkout.
/// </summary>
public enum CheckoutError
{
    /// <summary>Cart is empty and cannot be checked out.</summary>
    EmptyCart = 0,

    /// <summary>One or more products in the cart are out of stock.</summary>
    OutOfStock = 1,

    /// <summary>Payment processing failed.</summary>
    PaymentFailed = 2,
}
