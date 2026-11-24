// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing.given;

public record SharedOrder(
    EventSourceId Id,
    EventSourceId CustomerId,
    string CustomerName,
    string Label,
    string Summary,
    IEnumerable<SharedOrderLine> Lines);
