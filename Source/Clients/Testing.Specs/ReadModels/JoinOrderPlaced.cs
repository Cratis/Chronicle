// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event placing an order for a customer; lives on the order's own event source.
/// </summary>
/// <param name="CustomerId">The customer the order is for (the join key).</param>
/// <param name="Amount">The order amount.</param>
[EventType]
public record JoinOrderPlaced(JoinCustomerId CustomerId, decimal Amount);
