// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing.given;

/// <summary>
/// An order was placed for a customer.
/// </summary>
/// <param name="CustomerId">The customer that placed it.</param>
/// <param name="Label">What the order is called.</param>
[EventType]
public record SharedOrderPlaced(EventSourceId CustomerId, string Label);
