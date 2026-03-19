// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ECommerce.Cart;

namespace ECommerce.Orders;

/// <summary>
/// Read model representing an order.
/// </summary>
/// <param name="OrderId">The unique identifier of the order.</param>
/// <param name="CustomerId">The identifier of the customer who placed the order.</param>
/// <param name="Status">The current status of the order.</param>
/// <param name="OrderedAt">The date and time when the order was placed.</param>
/// <param name="ShippingAddress">The shipping address for the order.</param>
[ReadModel]
[BelongsTo("OrderService")]
public record Order(
    OrderId OrderId,
    CustomerId CustomerId,
    OrderStatus Status,
    DateTimeOffset OrderedAt,
    string ShippingAddress)
{
    static readonly List<Order> _orders =
    [
        new(
            OrderId.New(),
            CustomerId.New(),
            OrderStatus.Pending,
            DateTimeOffset.UtcNow.AddHours(-2),
            "123 Main St, Springfield"),
        new(
            OrderId.New(),
            CustomerId.New(),
            OrderStatus.Shipped,
            DateTimeOffset.UtcNow.AddDays(-3),
            "456 Oak Ave, Shelbyville"),
    ];

    static readonly BehaviorSubject<IEnumerable<Order>> _allOrders = new(_orders);

    /// <summary>
    /// Gets all orders.
    /// </summary>
    /// <returns>An observable sequence of all orders.</returns>
    internal static ISubject<IEnumerable<Order>> AllOrders() => _allOrders;

    /// <summary>
    /// Gets a single order by its identifier.
    /// </summary>
    /// <param name="orderId">The order identifier to search for.</param>
    /// <returns>The matching order or null.</returns>
    internal static async Task<Order?> GetOrder(OrderId orderId)
    {
        await Task.CompletedTask;
        return _orders.Find(o => o.OrderId == orderId);
    }

    /// <summary>
    /// Gets all orders for a specific customer.
    /// </summary>
    /// <param name="customerId">The customer identifier to filter by.</param>
    /// <returns>A collection of orders for the customer.</returns>
    internal static async Task<IEnumerable<Order>> GetOrdersForCustomer(CustomerId customerId)
    {
        await Task.CompletedTask;
        return _orders.Where(o => o.CustomerId == customerId);
    }
}
