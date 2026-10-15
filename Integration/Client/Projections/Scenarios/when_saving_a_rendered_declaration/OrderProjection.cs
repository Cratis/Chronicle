// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_saving_a_rendered_declaration;

public class OrderProjection : IProjectionFor<Order>
{
    public void Define(IProjectionBuilderFor<Order> builder) => builder.From<OrderPlaced>(from => from
        .Set(order => order.Name).To(@event => @event.Name)
        .Set(order => order.IsActive).ToValue(false)
        .Set(order => order.Occurred).ToEventContextProperty(context => context.Occurred));
}
