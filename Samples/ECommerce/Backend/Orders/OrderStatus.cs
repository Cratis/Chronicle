// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Orders;

/// <summary>
/// Represents the current status of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>Order has been placed and is awaiting processing.</summary>
    Pending = 0,

    /// <summary>Order is being processed.</summary>
    Processing = 1,

    /// <summary>Order has been shipped.</summary>
    Shipped = 2,

    /// <summary>Order has been delivered.</summary>
    Delivered = 3,

    /// <summary>Order has been cancelled.</summary>
    Cancelled = 4,
}
