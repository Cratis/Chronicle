// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ECommerce.Orders;

/// <summary>
/// Represents reasons why an order cannot be cancelled.
/// </summary>
public enum CancellationError
{
    /// <summary>Order has already been shipped and cannot be cancelled.</summary>
    AlreadyShipped = 0,

    /// <summary>Order has already been delivered.</summary>
    AlreadyDelivered = 1,

    /// <summary>Order does not exist.</summary>
    OrderNotFound = 2,
}
