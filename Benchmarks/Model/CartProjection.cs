// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Projections;

namespace Benchmark.Model;

public class CartProjection : IProjectionFor<Cart>
{
    public const string ActualIdentifier = "00ae5265-404f-4a6d-9dcd-87c6c856dfcf";

    public ProjectionId Identifier => ActualIdentifier;

    public void Define(IProjectionBuilderFor<Cart> builder) => builder
        .Children(_ => _.Items, cb => cb
            .IdentifiedBy(m => m.MaterialId)
            .From<ItemAddedToCart>(_ => _
                .UsingKey(e => e.MaterialId)
                .Set(m => m.Quantity).To(e => e.Quantity))
            .RemovedWith<ItemRemovedFromCart>()
            .From<QuantityAdjustedForItemInCart>(_ => _
                .UsingKey(e => e.MaterialId)
                .Set(m => m.Quantity).To(e => e.Quantity)));
}
