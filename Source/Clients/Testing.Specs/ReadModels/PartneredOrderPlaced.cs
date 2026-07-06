// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event placing an order for a customer; carries the order's own <see cref="Status"/>, the
/// authoritative source of the read model's status.
/// </summary>
/// <param name="CustomerId">The customer/partner the order is for (the join key).</param>
/// <param name="Status">The order status.</param>
[EventType]
public record PartneredOrderPlaced(JoinCustomerId CustomerId, string Status);
