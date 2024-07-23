// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Basic;

public class OrderStateProjection : IProjectionFor<OrderState>
{
    public ProjectionId Identifier => "4c6f7eac-d74d-425b-b2fd-e32e8e365b32";

    public void Define(IProjectionBuilderFor<OrderState> builder) => builder
        .Children(_ => _.CartItems, cb => cb
            .IdentifiedBy(m => m.MaterialId)
            .From<ItemAddedToCart>(_ => _
                .UsingKey(e => e.MaterialId)
                .Set(m => m.Quantity).To(e => e.Quantity))
            .RemovedWith<ItemRemovedFromCart>()
            .From<QuantityAdjustedForItemInCart>(_ => _
                .UsingKey(e => e.MaterialId)
                .Set(m => m.Quantity).To(e => e.Quantity)));
}
