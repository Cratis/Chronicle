// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing.given;

/// <summary>
/// A line was added to an order.
/// </summary>
/// <param name="OrderId">The order the line belongs to.</param>
/// <param name="Sku">The product on the line.</param>
/// <param name="Quantity">How many were ordered.</param>
[EventType]
public record SharedLineAdded(EventSourceId OrderId, string Sku, int Quantity);
