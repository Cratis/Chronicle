// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_join_and_no_auto_map;

/// <summary>
/// Root event placing an order for a customer; carries the order's own <see cref="Status"/>.
/// </summary>
/// <param name="CustomerId">The customer the order is for (the join key).</param>
/// <param name="Status">The order status — the authoritative source of the read model's status.</param>
[EventType]
public record OrderPlacedForCustomer(Guid CustomerId, string Status);
