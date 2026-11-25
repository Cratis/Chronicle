// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Kernel.Integration.Projections.for_ProjectionPlanSharing.given;

/// <summary>
/// Projects orders with a joined customer name and child lines.
/// </summary>
public class SharedOrderProjection : IProjectionFor<SharedOrder>
{
    /// <inheritdoc/>
    public void Define(IProjectionBuilderFor<SharedOrder> builder) => builder
        .AutoMap()
        .From<SharedOrderPlaced>(from => from.Set(order => order.Label).To(@event => @event.Label))
        .Join<SharedCustomerRegistered>(join => join
            .On(order => order.CustomerId)
            .Set(order => order.CustomerName).To(@event => @event.Name))
        .Children(order => order.Lines, children => children
            .IdentifiedBy(line => line.Sku)
            .From<SharedLineAdded>(from => from.UsingKey(@event => @event.Sku).UsingParentKey(@event => @event.OrderId)));
}
