// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ECommerce.Catalog;

namespace ECommerce.Cart;

/// <summary>
/// Read model representing an item in a shopping cart.
/// </summary>
/// <param name="CartId">The identifier of the cart.</param>
/// <param name="ProductId">The identifier of the product.</param>
/// <param name="ProductName">The name of the product.</param>
/// <param name="Quantity">The quantity of the product in the cart.</param>
/// <param name="UnitPrice">The unit price of the product.</param>
[ReadModel]
[BelongsTo("CartService")]
public record CartItem(CartId CartId, ProductId ProductId, ProductName ProductName, int Quantity, ProductPrice UnitPrice)
{
    static readonly Dictionary<CartId, List<CartItem>> _cartItems = [];
    static readonly Dictionary<CartId, BehaviorSubject<IEnumerable<CartItem>>> _cartSubjects = [];

    /// <summary>
    /// Observes all items in a specific cart.
    /// </summary>
    /// <param name="cartId">The cart identifier to observe.</param>
    /// <returns>An observable sequence of cart items.</returns>
    internal static ISubject<IEnumerable<CartItem>> GetCartItems(CartId cartId)
    {
        if (!_cartSubjects.TryGetValue(cartId, out var subject))
        {
            subject = new BehaviorSubject<IEnumerable<CartItem>>([]);
            _cartSubjects[cartId] = subject;
        }

        return subject;
    }

    /// <summary>
    /// Gets the total price for a cart.
    /// </summary>
    /// <param name="cartId">The cart identifier.</param>
    /// <returns>The total price of all items in the cart.</returns>
    internal static async Task<decimal> GetCartTotal(CartId cartId)
    {
        await Task.CompletedTask;
        if (!_cartItems.TryGetValue(cartId, out var items))
        {
            return 0m;
        }

        return items.Sum(i => (decimal)i.UnitPrice * i.Quantity);
    }
}
