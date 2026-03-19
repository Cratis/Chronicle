// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ECommerce.Orders;

namespace ECommerce.Cart;

/// <summary>
/// Represents a successful checkout result containing the new order id.
/// </summary>
/// <param name="OrderId">The identifier of the newly created order.</param>
public record CheckoutResult(OrderId OrderId);
