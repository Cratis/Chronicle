// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event placing an order that carries its own <see cref="Region"/> — a property name that collides
/// with the customer's region joined in from <see cref="RegionCustomerRegistered"/>. Lives on the order's
/// own event source.
/// </summary>
/// <param name="CustomerId">The customer the order is for (the join key).</param>
/// <param name="Region">The order's own region — a name collision with the joined-in customer region.</param>
[EventType]
public record RegionOrderPlaced(RegionCustomerId CustomerId, string Region);
